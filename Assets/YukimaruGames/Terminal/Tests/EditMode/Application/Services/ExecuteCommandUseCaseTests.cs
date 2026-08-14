using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.Entities;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Modes.Null;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Application.Services
{
    /// <summary>
    /// <see cref="ExecuteCommandUseCase"/> のディスパッチャとしての振る舞い
    /// (排他ロック・キャンセル/割り込み・モード遷移の適用・破棄処理)を検証するテストクラス。
    /// </summary>
    /// <remarks>
    /// 「1行の解釈」(パース/履歴/エコー)は <c>ExecutionModeTests</c> 側で検証済みのため、
    /// ここでは <see cref="TestMode"/> を使ってディスパッチャ固有の関心事のみを検証する。
    /// </remarks>
    [TestFixture]
    public sealed class ExecuteCommandUseCaseTests
    {
        // ─── Mocks ───────────────────────────────────────────────────────────

        private sealed class MockCommandLogger : ICommandLogger
        {
            public int MaxLogs => 100;
            public IReadOnlyCollection<CommandLog> Logs => Array.Empty<CommandLog>();
            public List<(MessageType type, string message)> Sent { get; } = new();

            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action OnItemUpdated { add { } remove { } }
            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<CommandLog[]> OnItemAdded { add { } remove { } }
            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<CommandLog[]> OnItemRemoved { add { } remove { } }

            public void Clear() { }
            public void Send(MessageType msgType, string message) => Sent.Add((msgType, message));
        }

        /// <summary>
        /// テスト用の可制御なモード. Push/Pop/割り込み/継続入力/例外の各シナリオを注入できる.
        /// </summary>
        private sealed class TestMode : ITerminalMode
        {
            public string Id { get; set; } = "test";
            public string Prompt => "test>";
            public string ContinuationPrompt => "...";
            public ICommandHistory History { get; set; } = NullCommandHistory.Instance;
            public ICommandAutocomplete Autocomplete { get; set; } = NullCommandAutocomplete.Instance;
            public bool AllowsConcurrentSpinner => false;

            public bool OnEnterCalled { get; private set; }
            public ModeExitReason? OnExitReason { get; private set; }
            public InterruptDisposition InterruptResult { get; set; } = InterruptDisposition.NotHandled;
            public Func<ModeInput, IModeContext, ValueTask<ModeResult>> OnHandle { get; set; }
            public Exception ThrowOnEnter { get; set; }
            public IModeContext LastContext { get; private set; }

            private TaskCompletionSource<bool> _handleGate;
            public void UseGate() => _handleGate = new TaskCompletionSource<bool>();
            public void OpenGate() => _handleGate?.TrySetResult(true);

            public async ValueTask OnEnterAsync(IModeContext context, CancellationToken cancellationToken)
            {
                if (ThrowOnEnter != null) throw ThrowOnEnter;
                OnEnterCalled = true;
                await Task.CompletedTask;
            }

            public async ValueTask<ModeResult> HandleAsync(ModeInput input, IModeContext context, CancellationToken cancellationToken)
            {
                LastContext = context;
                if (_handleGate != null)
                {
                    await _handleGate.Task;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (OnHandle != null)
                {
                    return await OnHandle(input, context);
                }

                return ModeResult.Continue;
            }

            public InterruptDisposition OnInterrupt(bool isCommandRunning) => InterruptResult;

            public ValueTask OnExitAsync(ModeExitReason reason)
            {
                OnExitReason = reason;
                return default;
            }
        }

        // ─── Setup ───────────────────────────────────────────────────────────

        private MockCommandLogger _logger;
        private TestMode _root;
        private ExecuteCommandUseCase _sut;

        [SetUp]
        public void SetUp()
        {
            _logger = new MockCommandLogger();
            _root = new TestMode { Id = "root" };
            _sut = new ExecuteCommandUseCase(_logger, _root);
        }

        private IExecuteCommandUseCase UseCase => _sut;

        // ─── IsExecuting / 排他ロック ────────────────────────────────────────

        [Test]
        public void IsExecuting_BeforeExecution_IsFalse()
        {
            Assert.IsFalse(_sut.IsExecuting);
        }

        [Test]
        public async Task IsExecuting_DuringHandleAsync_IsTrue()
        {
            _root.UseGate();
            var task = UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None).AsTask();

            await Task.Yield();
            Assert.IsTrue(_sut.IsExecuting);

            _root.OpenGate();
            await task;
            Assert.IsFalse(_sut.IsExecuting);
        }

        [Test]
        public async Task ExecutePipelineAsync_WhileExecuting_SecondCallIsIgnored()
        {
            var callCount = 0;
            _root.OnHandle = (_, _) => { callCount++; return new ValueTask<ModeResult>(ModeResult.Continue); };
            _root.UseGate();

            var first = UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None).AsTask();
            await Task.Yield();

            await UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            _root.OpenGate();
            await first;

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public async Task ExecutePipelineAsync_CancelledToken_DoesNotInvokeHandleAsync()
        {
            var called = false;
            _root.OnHandle = (_, _) => { called = true; return new ValueTask<ModeResult>(ModeResult.Continue); };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await UseCase.ExecutePipelineAsync("cmd".AsMemory(), cts.Token);

            Assert.IsFalse(called);
        }

        // ─── Interrupt ──────────────────────────────────────────────────────

        [Test]
        public void Interrupt_WhenNotExecuting_AtRoot_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => UseCase.Interrupt());
        }

        [Test]
        public async Task Interrupt_DuringExecution_CancelsCurrentCommandToken()
        {
            _root.OnHandle = async (_, _) =>
            {
                await Task.Yield();
                return ModeResult.Continue;
            };

            _root.UseGate();
            var task = UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None).AsTask();
            await Task.Yield();

            // 実行中にInterrupt() -> 内部CTSがCancelされる(HandleAsyncのctが観測できることは
            // TestMode.HandleAsync自体がcancellationToken.ThrowIfCancellationRequested()で保証する).
            UseCase.Interrupt();
            _root.OpenGate();

            await task;
            Assert.IsFalse(_sut.IsExecuting);
        }

        [Test]
        public async Task Interrupt_WhenNotExecuting_WithPushedMode_PopsOnNotHandled()
        {
            var child = new TestMode { Id = "child", InterruptResult = InterruptDisposition.NotHandled };
            await PushAsync(child);
            Assert.AreEqual(2, _sut.Depth);

            UseCase.Interrupt();
            await WaitUntil(() => _sut.Depth == 1);

            Assert.AreEqual(1, _sut.Depth);
            Assert.AreEqual(ModeExitReason.Interrupted, child.OnExitReason);
        }

        [Test]
        public async Task Interrupt_WhenNotExecuting_WithPushedMode_StaysOnHandled()
        {
            var child = new TestMode { Id = "child", InterruptResult = InterruptDisposition.Handled };
            await PushAsync(child);

            UseCase.Interrupt();
            await Task.Delay(50);

            Assert.AreEqual(2, _sut.Depth);
            Assert.IsNull(child.OnExitReason);
        }

        // ─── モード遷移(Push/Pop) ────────────────────────────────────────────

        [Test]
        public async Task Push_ViaSink_IncreasesDepthAndCallsOnEnter()
        {
            var child = new TestMode { Id = "child" };
            await PushAsync(child);

            Assert.AreEqual(2, _sut.Depth);
            Assert.IsTrue(child.OnEnterCalled);
        }

        [Test]
        public async Task Pop_ViaSink_ReturnsToRootAndCallsOnExit()
        {
            var child = new TestMode { Id = "child" };
            await PushAsync(child);

            child.OnHandle = (_, context) =>
            {
                context.Transitions.RequestPop();
                return new ValueTask<ModeResult>(ModeResult.Continue);
            };

            await UseCase.ExecutePipelineAsync("exit".AsMemory(), CancellationToken.None);

            Assert.AreEqual(1, _sut.Depth);
            Assert.AreEqual(ModeExitReason.Popped, child.OnExitReason);
        }

        [Test]
        public async Task Push_OnEnterThrows_DoesNotIncreaseDepth_AndCallsOnExitWithEnterFailed()
        {
            var child = new TestMode { Id = "child", ThrowOnEnter = new InvalidOperationException("boom") };
            await PushAsync(child);

            Assert.AreEqual(1, _sut.Depth);
            Assert.AreEqual(ModeExitReason.EnterFailed, child.OnExitReason);
        }

        [Test]
        public async Task RequestPop_ExceedingDepth_ClampsAtRoot()
        {
            var child = new TestMode { Id = "child" };
            await PushAsync(child);

            child.OnHandle = (_, context) =>
            {
                context.Transitions.RequestPop(count: 99);
                return new ValueTask<ModeResult>(ModeResult.Continue);
            };

            await UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.AreEqual(1, _sut.Depth);
        }

        // ─── 継続入力(NeedMoreInput) ─────────────────────────────────────────

        [Test]
        public async Task HandleAsync_NeedMoreInput_SetsIsAwaitingContinuation()
        {
            _root.OnHandle = (_, _) => new ValueTask<ModeResult>(ModeResult.NeedMoreInput);

            await UseCase.ExecutePipelineAsync("line1".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_sut.IsAwaitingContinuation);
            Assert.AreEqual(_root.ContinuationPrompt, _sut.Prompt);
        }

        [Test]
        public async Task HandleAsync_ContinuationCompletes_AccumulatesText()
        {
            string received = null;
            var first = true;
            _root.OnHandle = (input, _) =>
            {
                if (first)
                {
                    first = false;
                    return new ValueTask<ModeResult>(ModeResult.NeedMoreInput);
                }

                received = input.Text.ToString();
                return new ValueTask<ModeResult>(ModeResult.Continue);
            };

            await UseCase.ExecutePipelineAsync("line1".AsMemory(), CancellationToken.None);
            await UseCase.ExecutePipelineAsync("line2".AsMemory(), CancellationToken.None);

            Assert.AreEqual("line1\nline2", received);
            Assert.IsFalse(_sut.IsAwaitingContinuation);
        }

        // ─── 例外ハンドリング ────────────────────────────────────────────────

        [Test]
        public async Task HandleAsync_Throws_LogsExceptionAndDoesNotChangeMode()
        {
            _root.OnHandle = (_, _) => throw new InvalidOperationException("oops");

            await UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception && s.message.Contains(nameof(InvalidOperationException))));
            Assert.AreEqual(1, _sut.Depth);
        }

        [Test]
        public async Task HandleAsync_ThrowsAfterRequestingPush_DiscardsTheRequest()
        {
            _root.OnHandle = (_, context) =>
            {
                context.Transitions.RequestPush(new TestMode { Id = "child" });
                throw new InvalidOperationException("oops");
            };

            await UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.AreEqual(1, _sut.Depth);
        }

        // ─── Dispose ────────────────────────────────────────────────────────

        [Test]
        public async Task DisposeAsync_PopsAllPushedModes()
        {
            var child = new TestMode { Id = "child" };
            await PushAsync(child);

            await ((IAsyncDisposable)_sut).DisposeAsync();

            Assert.AreEqual(1, _sut.Depth);
            Assert.AreEqual(ModeExitReason.Shutdown, child.OnExitReason);
        }

        [Test]
        public async Task DisposeAsync_IsIdempotent()
        {
            await ((IAsyncDisposable)_sut).DisposeAsync();
            Assert.DoesNotThrowAsync(async () => await ((IAsyncDisposable)_sut).DisposeAsync());
        }

        [Test]
        public void ExecutePipelineAsync_AfterDispose_DoesNothing()
        {
            ((IDisposable)_sut).Dispose();
            Assert.DoesNotThrowAsync(async () => await UseCase.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None));
        }

        // ─── ヘルパー ────────────────────────────────────────────────────────

        private async Task PushAsync(TestMode child)
        {
            _root.OnHandle = (_, context) =>
            {
                context.Transitions.RequestPush(child);
                return new ValueTask<ModeResult>(ModeResult.Continue);
            };

            await UseCase.ExecutePipelineAsync("enter".AsMemory(), CancellationToken.None);

            // 以降の呼び出しでPushを繰り返さないよう解除
            _root.OnHandle = null;
        }

        private static async Task WaitUntil(Func<bool> predicate, int timeoutMs = 1000)
        {
            var elapsed = 0;
            while (!predicate() && elapsed < timeoutMs)
            {
                await Task.Delay(10);
                elapsed += 10;
            }
        }
    }
}

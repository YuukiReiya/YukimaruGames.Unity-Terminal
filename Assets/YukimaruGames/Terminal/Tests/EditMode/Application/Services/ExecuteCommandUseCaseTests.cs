using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Contracts.Exceptions;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.Entities;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Application.Services
{
    /// <summary>
    /// <see cref="ExecuteCommandUseCase"/> の一連の処理パイプラインを検証するテストクラス。
    /// </summary>
    [TestFixture]
    public sealed class ExecuteCommandUseCaseTests
    {
        // ─── Mocks ───────────────────────────────────────────────────────────

        private sealed class MockCommandLogger : ICommandLogger
        {
            public int MaxLogs => 100;
            public IReadOnlyCollection<CommandLog> Logs => _logs;
            // ReSharper disable once CollectionNeverUpdated.Local
            private readonly List<CommandLog> _logs = new ();
            public List<(MessageType type, string message)> Sent { get; } = new();

            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action OnItemUpdated { add { } remove { } }
            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<CommandLog[]> OnItemAdded { add { } remove { } }
            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<CommandLog[]> OnItemRemoved { add { } remove { } }

            public void Clear() => _logs.Clear();
            public void Send(MessageType msgType, string message) => Sent.Add((msgType, message));
        }

        private sealed class MockCommandRegistry : ICommandRegistry
        {
            private readonly Dictionary<string, CommandHandler> _handlers = new();

            public bool Add(string command, CommandHandler handle) { _handlers[command] = handle; return true; }
            public bool Remove(string command) => _handlers.Remove(command);
            public bool TryGet(string command, out CommandHandler handler) => _handlers.TryGetValue(command, out handler);
        }

        private sealed class MockCommandInvoker : ICommandInvoker
        {
            public bool ExecuteCalled { get; private set; }
            public bool ExecuteAsyncCalled { get; private set; }
            public int ExecuteAsyncCallCount { get; private set; }
            public bool CancellationObserved { get; private set; }
            public Exception ThrowException { get; set; }

            private TaskCompletionSource<bool> _executeAsyncGate;
            public void OpenGate() => _executeAsyncGate?.TrySetResult(true);
            public void UseGate() => _executeAsyncGate = new TaskCompletionSource<bool>();

            public void Execute(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments)
            {
                ExecuteCalled = true;
                if (ThrowException != null) throw ThrowException;
            }

            public async ValueTask ExecuteAsync(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments, CancellationToken cancellationToken)
            {
                ExecuteAsyncCalled = true;
                ExecuteAsyncCallCount++;
                if (_executeAsyncGate != null)
                {
                    await _executeAsyncGate.Task;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    CancellationObserved = true;
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (ThrowException != null) throw ThrowException;
            }
        }

        private sealed class MockCommandParser : ICommandParser
        {
            public string CommandName { get; set; }
            private CommandArgument[] Arguments { get; } = Array.Empty<CommandArgument>();
            public ICommandParser.ParseStatusCode StatusCode { get; set; } = ICommandParser.ParseStatusCode.Ok;

            public ICommandParser.ParseStatusCode Parse(string str, out (string Command, CommandArgument[] Arguments) tuple)
            {
                tuple = (CommandName, Arguments);
                return StatusCode;
            }

            ICommandParser.ParseStatusCode ICommandParser.Parse(ReadOnlyMemory<char> str, out (string Command, CommandArgument[] Arguments) tuple)
            {
                tuple = (CommandName, Arguments);
                return StatusCode;
            }

            ValueTask<(ICommandParser.ParseStatusCode Status, string Command, CommandArgument[] Arguments)> ICommandParser.ParseAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new((StatusCode, CommandName, Arguments));
            }
        }

        private sealed class MockCommandHistory : ICommandHistory
        {
            public List<string> Histories { get; } = new();
            IReadOnlyCollection<string> ICommandHistory.Histories => Histories;
            public void Clear() => Histories.Clear();
            public bool Add(string str) { Histories.Add(str); return true; }
            public string Next() => null;
            public string Previous() => null;
        }

        // ─── Setup ───────────────────────────────────────────────────────────

        private MockCommandLogger _logger;
        private MockCommandRegistry _registry;
        private MockCommandInvoker _invoker;
        private MockCommandParser _parser;
        private MockCommandHistory _history;
        private ExecuteCommandUseCase _sut;

        private static readonly CommandHandler SyncHandler =
            new((_) => { }, "cmd", 0, 0, "");

        private static readonly CommandHandler AsyncHandler =
            new ((_, _) => default, "cmd", 0, 0, "");

        [SetUp]
        public void SetUp()
        {
            _logger   = new MockCommandLogger();
            _registry = new MockCommandRegistry();
            _invoker  = new MockCommandInvoker();
            _parser   = new MockCommandParser();
            _history  = new MockCommandHistory();
            _sut = new ExecuteCommandUseCase(_logger, _registry, _invoker, _parser, _history);
        }

        // ─── ExecutePipelineAsync: 正常系 ─────────────────────────────────────

        /// <summary>
        /// 有効な同期コマンドが入力された際、パイプライン経由で同期ハンドラーが呼び出されることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_ValidSyncCommand_InvokesExecute()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
            Assert.IsFalse(_invoker.ExecuteAsyncCalled);
        }

        /// <summary>
        /// 有効な非同期コマンドが入力された際、パイプライン経由で非同期ハンドラーが呼び出されることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_ValidAsyncCommand_InvokesExecuteAsync()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteAsyncCalled);
            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        /// <summary>
        /// パイプライン経由での実行時、有効なコマンドであれば正常に履歴（History）レポジトリに保存されることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_ValidCommand_AddsToHistory()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            CollectionAssert.Contains(_history.Histories, "cmd");
        }

        /// <summary>
        /// コマンドの実行開始時、パイプラインによってエントリログが送信されることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_ValidCommand_LogsEntry()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Entry));
        }

        // ─── ExecutePipelineAsync: 異常系 ─────────────────────────────────────

        /// <summary>
        /// 未知のコマンド名が入力された場合、実行されずにエラーログが出力されることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_UnknownCommand_DoesNotInvoke()
        {
            _parser.CommandName = "unknown";

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("unknown".AsMemory(), CancellationToken.None);

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        /// <summary>
        /// 空文字など、コマンド名が抽出されなかった入力に対しては、何も処理を行わずに早期リターンすることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_EmptyCommand_DoesNotInvoke()
        {
            _parser.CommandName = null;

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync(ReadOnlyMemory<char>.Empty, CancellationToken.None);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        /// <summary>
        /// 構文エラー（SyntaxError）が発生した場合は実行を拒否し、エラーログを出力することを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_SyntaxError_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _parser.StatusCode  = ICommandParser.ParseStatusCode.SyntaxError;
            _registry.Add("cmd", SyncHandler);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        // ─── IsExecuting ──────────────────────────────────────────────────────

        [Test]
        public void IsExecuting_BeforeExecution_IsFalse()
        {
            Assert.IsFalse(_sut.IsExecuting);
        }

        /// <summary>
        /// 非同期コマンドの処理が実行されている「最中」は、実行中フラグが確実に True になることを検証します。
        /// </summary>
        [Test]
        public async Task IsExecuting_DuringAsyncExecution_IsTrue()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.UseGate();

            var use = (IExecuteCommandUseCase)_sut;
            var task = use.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None).AsTask();

            await Task.Yield();
            Assert.IsTrue(_sut.IsExecuting);

            _invoker.OpenGate();
            await task;
        }

        [Test]
        public async Task IsExecuting_AfterExecution_IsFalse()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsFalse(_sut.IsExecuting);
        }

        /// <summary>
        /// 多重実行要求が走った場合、その2回目は完全に無視されることを検証します。
        /// </summary>
        [Test]
        public async Task ExecutePipelineAsync_WhileExecuting_SecondCallIsIgnored()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.UseGate();

            var use = (IExecuteCommandUseCase)_sut;
            var first = use.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None).AsTask();

            await Task.Yield();

            // 実行中に2回目を呼ぶ → 無視される
            await use.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            _invoker.OpenGate();
            await first;

            Assert.AreEqual(1, _invoker.ExecuteAsyncCallCount);
        }

        // ─── CancelCommandIfNeeded ────────────────────────────────────────────

        [Test]
        public void CancelCommandIfNeeded_WhenNotExecuting_DoesNotThrow()
        {
            var use = (IExecuteCommandUseCase)_sut;
            Assert.DoesNotThrow(() => use.CancelCommandIfNeeded());
        }

        /// <summary>
        /// 実行中に外部からキャンセルが要求された場合、内部トークンが連動してキャンセルされ、例外が安全に吸収されることを検証します。
        /// </summary>
        [Test]
        public async Task CancelCommandIfNeeded_DuringAsyncExecution_CancelsCommand()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.UseGate();

            var use = (IExecuteCommandUseCase)_sut;
            var task = use.ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None).AsTask();

            await Task.Yield();

            use.CancelCommandIfNeeded();
            _invoker.OpenGate();

            await task; 

            Assert.IsTrue(_invoker.CancellationObserved);
            Assert.IsFalse(_sut.IsExecuting);
        }

        // ─── CancellationToken ────────────────────────────────────────────────

        [Test]
        public async Task ExecutePipelineAsync_CancelledToken_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), cts.Token);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        // ─── 例外ハンドリング ─────────────────────────────────────────────────

        [Test]
        public async Task ExecutePipelineAsync_CommandArgumentException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new CommandArgumentException(3, 1, 2, null);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        [Test]
        public async Task ExecutePipelineAsync_CommandFormatException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new CommandFormatException(0, "bad", typeof(int), null);

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        [Test]
        public async Task ExecutePipelineAsync_UnexpectedException_LogsExceptionWithTypeName()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new InvalidOperationException("oops");

            await ((IExecuteCommandUseCase)_sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_logger.Sent.Exists(s =>
                s.type == MessageType.Exception &&
                s.message.Contains(nameof(InvalidOperationException))));
        }

        // ─── null logger 許容 ────────────────────────────────────────────────

        [Test]
        public void ExecutePipelineAsync_NullLogger_DoesNotThrow()
        {
            var sut = new ExecuteCommandUseCase(null, _registry, _invoker, _parser, _history);
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            Assert.DoesNotThrowAsync(async () =>
                await ((IExecuteCommandUseCase)sut).ExecutePipelineAsync("cmd".AsMemory(), CancellationToken.None));
        }
    }
}

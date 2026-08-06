using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.Entities;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Services
{
    /// <summary>
    /// <see cref="NormalMode"/> のパース・履歴・エコー・実行委譲を検証するテストクラス。
    /// </summary>
    /// <remarks>
    /// 旧 <c>ExecuteCommandUseCaseTests</c> が検証していた「1行の解釈」に関するテスト群の移送先。
    /// ディスパッチャ固有の関心事(排他ロック・キャンセル伝播・多重実行無視等)は
    /// <c>ExecuteCommandUseCaseTests</c> 側に残る(Phase4でディスパッチャ化に合わせて更新予定)。
    /// </remarks>
    [TestFixture]
    public sealed class NormalModeTests
    {
        // ─── Mocks ───────────────────────────────────────────────────────────

        private sealed class MockCommandLogger : ICommandLogger
        {
            public int MaxLogs => 100;
            public IReadOnlyCollection<CommandLog> Logs => _logs;
            private readonly List<CommandLog> _logs = new();
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

            public bool Add(string command, CommandHandler handle)
            {
                _handlers[command] = handle;
                return true;
            }

            public bool Remove(string command) => _handlers.Remove(command);
            public bool TryGet(string command, out CommandHandler handler) => _handlers.TryGetValue(command, out handler);
        }

        private sealed class MockCommandInvoker : ICommandInvoker
        {
            public bool ExecuteCalled { get; private set; }
            public bool ExecuteAsyncCalled { get; private set; }
            public Exception ThrowException { get; set; }

            public void Execute(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments)
            {
                ExecuteCalled = true;
                if (ThrowException != null) throw ThrowException;
            }

            public ValueTask ExecuteAsync(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments, CancellationToken cancellationToken)
            {
                ExecuteAsyncCalled = true;
                if (ThrowException != null) throw ThrowException;
                return default;
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
            public string Next() => string.Empty;
            public string Previous() => string.Empty;
        }

        // ─── Setup ───────────────────────────────────────────────────────────

        private MockCommandLogger _logger;
        private MockCommandRegistry _registry;
        private MockCommandInvoker _invoker;
        private MockCommandParser _parser;
        private MockCommandHistory _history;
        private NormalMode _sut;

        private static readonly CommandHandler SyncHandler = new(_ => { }, "cmd", 0, 0, "");
        private static readonly CommandHandler AsyncHandler = new((_, _) => default, "cmd", 0, 0, "");

        [SetUp]
        public void SetUp()
        {
            _logger = new MockCommandLogger();
            _registry = new MockCommandRegistry();
            _invoker = new MockCommandInvoker();
            _parser = new MockCommandParser();
            _history = new MockCommandHistory();
            _sut = new NormalMode(_logger, _registry, _invoker, _parser, _history, autocomplete: null);
        }

        private ValueTask<ModeResult> HandleAsync(string text, CancellationToken cancellationToken = default)
        {
            var input = new ModeInput(text.AsMemory(), isContinuation: false);
            return _sut.HandleAsync(input, context: null, cancellationToken);
        }

        // ─── 正常系 ─────────────────────────────────────

        [Test]
        public async Task HandleAsync_ValidSyncCommand_InvokesExecute()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await HandleAsync("cmd");

            Assert.IsTrue(_invoker.ExecuteCalled);
            Assert.IsFalse(_invoker.ExecuteAsyncCalled);
        }

        [Test]
        public async Task HandleAsync_ValidAsyncCommand_InvokesExecuteAsync()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            await HandleAsync("cmd");

            Assert.IsTrue(_invoker.ExecuteAsyncCalled);
            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        [Test]
        public async Task HandleAsync_ValidCommand_ReturnsContinue()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            var result = await HandleAsync("cmd");

            Assert.AreEqual(ModeResult.Continue, result);
        }

        [Test]
        public async Task HandleAsync_ValidCommand_AddsToHistory()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await HandleAsync("cmd");

            CollectionAssert.Contains(_history.Histories, "cmd");
        }

        [Test]
        public async Task HandleAsync_ValidCommand_LogsEntry()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await HandleAsync("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Entry));
        }

        // ─── 異常系 ─────────────────────────────────────

        [Test]
        public async Task HandleAsync_UnknownCommand_DoesNotInvoke()
        {
            _parser.CommandName = "unknown";

            await HandleAsync("unknown");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        [Test]
        public async Task HandleAsync_EmptyCommand_DoesNotInvoke()
        {
            _parser.CommandName = null;

            await HandleAsync(string.Empty);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        [Test]
        public async Task HandleAsync_SyntaxError_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _parser.StatusCode = ICommandParser.ParseStatusCode.SyntaxError;
            _registry.Add("cmd", SyncHandler);

            await HandleAsync("cmd");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        [Test]
        public async Task HandleAsync_CancelledToken_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await HandleAsync("cmd", cts.Token);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        // ─── 例外はディスパッチャに委ねる(ここでは捕捉しない) ───────────────

        [Test]
        public void HandleAsync_InvokerThrows_PropagatesException()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new InvalidOperationException("oops");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await HandleAsync("cmd"));
        }

        [Test]
        public void HandleAsync_AsyncInvokerThrows_PropagatesException()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.ThrowException = new InvalidOperationException("oops");

            Assert.ThrowsAsync<InvalidOperationException>(async () => await HandleAsync("cmd"));
        }

        // ─── null logger 許容 ────────────────────────────────────────────────

        [Test]
        public void HandleAsync_NullLogger_DoesNotThrow()
        {
            var sut = new NormalMode(null, _registry, _invoker, _parser, _history, autocomplete: null);
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            Assert.DoesNotThrowAsync(async () =>
            {
                var input = new ModeInput("cmd".AsMemory(), isContinuation: false);
                await sut.HandleAsync(input, context: null, CancellationToken.None);
            });
        }

        // ─── メタ情報 ────────────────────────────────────────────────────────

        [Test]
        public void Id_IsNormal()
        {
            Assert.AreEqual("normal", _sut.Id);
        }

        [Test]
        public void History_ReturnsInjectedInstance()
        {
            Assert.AreSame(_history, _sut.History);
        }

        [Test]
        public void Autocomplete_NullInjected_ReturnsNullObject()
        {
            var sut = new NormalMode(_logger, _registry, _invoker, _parser, _history, autocomplete: null);
            Assert.IsNotNull(sut.Autocomplete);
        }

        [Test]
        public void History_NullInjected_ReturnsNullObject()
        {
            var sut = new NormalMode(_logger, _registry, _invoker, _parser, history: null, autocomplete: null);
            Assert.IsNotNull(sut.History);
        }

        [Test]
        public void Prompt_ReturnsNonEmpty()
        {
            Assert.IsNotEmpty(_sut.Prompt);
            Assert.IsNotEmpty(_sut.ContinuationPrompt);
        }

        [Test]
        public void AllowsConcurrentSpinner_DefaultsToFalse()
        {
            Assert.IsFalse(_sut.AllowsConcurrentSpinner);
        }

        [Test]
        public void OnInterrupt_ReturnsNotHandled()
        {
            Assert.AreEqual(InterruptDisposition.NotHandled, _sut.OnInterrupt(isCommandRunning: false));
        }
    }
}

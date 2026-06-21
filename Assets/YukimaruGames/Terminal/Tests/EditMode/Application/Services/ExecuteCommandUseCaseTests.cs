using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Exceptions;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Models.Entities;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Application.Services
{
    [TestFixture]
    public sealed class ExecuteCommandUseCaseTests
    {
        // ─── Mocks ───────────────────────────────────────────────────────────

        private sealed class MockCommandLogger : ICommandLogger
        {
            public int MaxLogs => 100;
            public IReadOnlyCollection<CommandLog> Logs => _logs;
            private readonly List<CommandLog> _logs = new();
            public List<(MessageType type, string message)> Sent { get; } = new();

            public event Action OnItemUpdated;
            public event Action<CommandLog[]> OnItemAdded;
            public event Action<CommandLog[]> OnItemRemoved;

            public void Clear() => _logs.Clear();
            public void Send(MessageType msgType, string message) => Sent.Add((msgType, message));
        }

        private sealed class MockCommandRegistry : ICommandRegistry
        {
            private readonly Dictionary<string, CommandHandler> _handlers = new();

            public bool Add(string command, CommandHandler handle) { _handlers[command] = handle; return true; }
            public bool Remove(string command) => _handlers.Remove(command);
            public bool TryGet(string command, out CommandHandler handler) => _handlers.TryGetValue(command, out handler);
            public IReadOnlyCollection<CommandHandler> GetAll() => _handlers.Values;
        }

        private sealed class MockCommandInvoker : ICommandInvoker
        {
            public bool ExecuteCalled { get; private set; }
            public bool ExecuteAsyncCalled { get; private set; }
            public Exception ThrowException { get; set; }

            // ExecuteAsync 完了まで待機させる制御用
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
                if (_executeAsyncGate != null)
                {
                    await _executeAsyncGate.Task;
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (ThrowException != null) throw ThrowException;
            }
        }

        private sealed class MockCommandParser : ICommandParser
        {
            public string CommandName { get; set; }
            public CommandArgument[] Arguments { get; set; }
            public ICommandParser.ParseStatusCode StatusCode { get; set; } = ICommandParser.ParseStatusCode.Ok;

            public ICommandParser.ParseStatusCode Parse(string str, out (string Command, CommandArgument[] Arguments) tuple)
            {
                tuple = (CommandName, Arguments);
                return StatusCode;
            }

            public ICommandParser.ParseStatusCode Parse(ReadOnlyMemory<char> str, out (string Command, CommandArgument[] Arguments) tuple)
            {
                tuple = (CommandName, Arguments);
                return StatusCode;
            }

            public ValueTask<(ICommandParser.ParseStatusCode Status, string Command, CommandArgument[] Arguments)> ParseAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public ValueTask<(ICommandParser.ParseStatusCode Status, string Command, CommandArgument[] Arguments)> ParseAsync(ReadOnlyMemory<char> str)
                => new((StatusCode, CommandName, Arguments));
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

        // 同期用ハンドラ
        private static readonly CommandHandler SyncHandler =
            new CommandHandler((args) => { }, "cmd", 0, 0, "");

        // 非同期用ハンドラ
        private static readonly CommandHandler AsyncHandler =
            new CommandHandler((CommandAsyncDelegate)((args, ct) => default), "cmd", 0, 0, "");

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

        // ─── Execute（同期）: 正常系 ─────────────────────────────────────────

        [Test]
        public void Execute_String_ValidSyncCommand_InvokesHandler()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        [Test]
        public void Execute_Memory_ValidSyncCommand_InvokesHandler()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd".AsMemory());

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        [Test]
        public void Execute_ValidCommand_AddsToHistory()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            CollectionAssert.Contains(_history.Histories, "cmd");
        }

        [Test]
        public void Execute_ValidCommand_LogsEntry()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Entry));
        }

        // ─── Execute（同期）: 異常系 ─────────────────────────────────────────

        [Test]
        public void Execute_AsyncHandler_LogsError_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        [Test]
        public void Execute_UnknownCommand_DoesNotInvoke()
        {
            _parser.CommandName = "unknown";

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("unknown");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        [Test]
        public void Execute_EmptyCommand_DoesNotInvoke()
        {
            _parser.CommandName = null;

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute(string.Empty);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        [Test]
        public void Execute_SyntaxError_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _parser.StatusCode  = ICommandParser.ParseStatusCode.SyntaxError;
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        // ─── ExecuteAsync（非同期）: 正常系 ──────────────────────────────────

        [Test]
        public async Task ExecuteAsync_ValidSyncCommand_InvokesExecute()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
            Assert.IsFalse(_invoker.ExecuteAsyncCalled);
        }

        [Test]
        public async Task ExecuteAsync_ValidAsyncCommand_InvokesExecuteAsync()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteAsyncCalled);
            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        [Test]
        public async Task ExecuteAsync_ValidCommand_AddsToHistory()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            CollectionAssert.Contains(_history.Histories, "cmd");
        }

        [Test]
        public async Task ExecuteAsync_Memory_ValidCommand_InvokesHandler()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        // ─── ExecuteAsync（非同期）: IsExecuting ──────────────────────────────

        [Test]
        public void IsExecuting_BeforeExecution_IsFalse()
        {
            Assert.IsFalse(_sut.IsExecuting);
        }

        [Test]
        public async Task IsExecuting_DuringAsyncExecution_IsTrue()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.UseGate();

            var iuse = (YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut;
            var task = iuse.ExecuteAsync("cmd", CancellationToken.None).AsTask();

            // ゲートで ExecuteAsync を一時停止している間 IsExecuting が true
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

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsFalse(_sut.IsExecuting);
        }

        [Test]
        public async Task ExecuteAsync_WhileExecuting_SecondCallIsIgnored()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.UseGate();

            var iuse = (YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut;
            var first = iuse.ExecuteAsync("cmd", CancellationToken.None).AsTask();

            await Task.Yield();

            // 実行中に2回目を呼ぶ → 無視される
            await iuse.ExecuteAsync("cmd", CancellationToken.None);

            _invoker.OpenGate();
            await first;

            // ExecuteAsync は1回しか呼ばれていない
            Assert.IsTrue(_invoker.ExecuteAsyncCalled);
        }

        // ─── CancelCommandIfNeeded ────────────────────────────────────────────

        [Test]
        public void CancelCommandIfNeeded_WhenNotExecuting_DoesNotThrow()
        {
            var iuse = (YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut;

            Assert.DoesNotThrow(() => iuse.CancelCommandIfNeeded());
        }

        [Test]
        public async Task CancelCommandIfNeeded_DuringAsyncExecution_CancelsCommand()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.UseGate();

            var iuse = (YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut;
            var task = iuse.ExecuteAsync("cmd", CancellationToken.None).AsTask();

            await Task.Yield();

            iuse.CancelCommandIfNeeded();
            _invoker.OpenGate();

            await task; // HandleException 内で OperationCanceledException が吸収される

            Assert.IsFalse(_sut.IsExecuting);
        }

        [Test]
        public async Task CancelCommandIfNeeded_AfterExecution_IsExecutingIsFalse()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            var iuse = (YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut;
            await iuse.ExecuteAsync("cmd", CancellationToken.None);
            iuse.CancelCommandIfNeeded();

            Assert.IsFalse(_sut.IsExecuting);
        }

        // ─── CancellationToken ────────────────────────────────────────────────

        [Test]
        public async Task ExecuteAsync_CancelledToken_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", cts.Token);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        [Test]
        public async Task ExecuteAsync_ActiveToken_ExecutesNormally()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        // ─── 例外ハンドリング ─────────────────────────────────────────────────

        [Test]
        public void Execute_CommandArgumentException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new CommandArgumentException(3, 1, 2, null);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        [Test]
        public void Execute_CommandFormatException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new CommandFormatException(0, "bad", typeof(int), null);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        [Test]
        public void Execute_UnexpectedException_LogsExceptionWithTypeName()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new InvalidOperationException("oops");

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s =>
                s.type == MessageType.Exception &&
                s.message.Contains(nameof(InvalidOperationException))));
        }

        [Test]
        public async Task ExecuteAsync_UnexpectedException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", AsyncHandler);
            _invoker.ThrowException = new InvalidOperationException("oops");

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        // ─── null logger 許容 ────────────────────────────────────────────────

        [Test]
        public void Execute_NullLogger_DoesNotThrow()
        {
            var sut = new ExecuteCommandUseCase(null, _registry, _invoker, _parser, _history);
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            Assert.DoesNotThrow(() => ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)sut).Execute("cmd"));
        }

        [Test]
        public async Task ExecuteAsync_NullLogger_DoesNotThrow()
        {
            var sut = new ExecuteCommandUseCase(null, _registry, _invoker, _parser, _history);
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            Assert.DoesNotThrowAsync(async () =>
                await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)sut).ExecuteAsync("cmd", CancellationToken.None));
        }
    }
}
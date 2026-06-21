// using System;
// using System.Collections.Generic;
// using System.Threading;
// using System.Threading.Tasks;
// using NUnit.Framework;
// using YukimaruGames.Terminal.Application.Interfaces;
// using YukimaruGames.Terminal.Application.Services;
// using YukimaruGames.Terminal.Domain.Abstractions.Exceptions;
// using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Repositories;
// using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
// using YukimaruGames.Terminal.Domain.Abstractions.Models.Entities;
// using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
// using YukimaruGames.Terminal.SharedKernel;
//
// namespace YukimaruGames.Terminal.Tests.EditMode.Application.Services
// {
//     [TestFixture]
//     public sealed class ExecuteCommandUseCaseTests
//     {
//         // ─── Mocks ───────────────────────────────────────────────────────────
//
//         private sealed class MockCommandLogger : ICommandLogger
//         {
//             public int MaxLogs => 100;
//             public IReadOnlyCollection<CommandLog> Logs => _logs;
//             private readonly List<CommandLog> _logs = new();
//             private readonly List<(MessageType type, string message)> _sent = new();
//
//             public event Action OnItemUpdated;
//             public event Action<CommandLog[]> OnItemAdded;
//             public event Action<CommandLog[]> OnItemRemoved;
//
//             public IReadOnlyList<(MessageType type, string message)> Sent => _sent;
//
//             public void Clear() => _logs.Clear();
//
//             public void Send(MessageType msgType, string message)
//                 => _sent.Add((msgType, message));
//         }
//
//         private sealed class MockCommandRegistry : ICommandRegistry
//         {
//             private readonly Dictionary<string, CommandHandler> _handlers = new();
//
//             public bool Add(string command, CommandHandler handle)
//             {
//                 _handlers[command] = handle;
//                 return true;
//             }
//
//             public bool Remove(string command) => _handlers.Remove(command);
//
//             public bool TryGet(string command, out CommandHandler handler)
//                 => _handlers.TryGetValue(command, out handler);
//
//             public IReadOnlyCollection<CommandHandler> GetAll() => _handlers.Values;
//         }
//
//         private sealed class MockCommandInvoker : ICommandInvoker
//         {
//             public bool WasCalled { get; private set; }
//             public Exception ThrowException { get; set; }
//
//             public void Execute(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments)
//             {
//                 WasCalled = true;
//                 if (ThrowException != null) throw ThrowException;
//             }
//         }
//
//         private sealed class MockCommandParser : ICommandParser
//         {
//             public string CommandName { get; set; }
//             public CommandArgument[] Arguments { get; set; }
//             public ICommandParser.ParseStatusCode StatusCode { get; set; }
//                 = ICommandParser.ParseStatusCode.Ok;
//
//             public ICommandParser.ParseStatusCode Parse(
//                 string str,
//                 out (string Command, CommandArgument[] Arguments) tuple)
//             {
//                 tuple = (CommandName, Arguments);
//                 return StatusCode;
//             }
//
//             public ICommandParser.ParseStatusCode Parse(
//                 ReadOnlyMemory<char> str,
//                 out (string Command, CommandArgument[] Arguments) tuple)
//             {
//                 tuple = (CommandName, Arguments);
//                 return StatusCode;
//             }
//
//             public ValueTask<(ICommandParser.ParseStatusCode Status, string Command, CommandArgument[] Arguments)>
//                 ParseAsync(ReadOnlyMemory<char> str)
//                 => new((StatusCode, CommandName, Arguments));
//         }
//
//         private sealed class MockCommandHistory : ICommandHistory
//         {
//             public IReadOnlyCollection<string> Histories => _histories;
//             private readonly List<string> _histories = new();
//
//             public void Clear() => _histories.Clear();
//             public bool Add(string str) { _histories.Add(str); return true; }
//             public string Next() => null;
//             public string Previous() => null;
//         }
//
//         // ─── Setup ───────────────────────────────────────────────────────────
//
//         private MockCommandLogger _logger;
//         private MockCommandRegistry _registry;
//         private MockCommandInvoker _invoker;
//         private MockCommandParser _parser;
//         private MockCommandHistory _history;
//         private IExecuteCommandUseCase _sut;
//
//         [SetUp]
//         public void SetUp()
//         {
//             _logger   = new MockCommandLogger();
//             _registry = new MockCommandRegistry();
//             _invoker  = new MockCommandInvoker();
//             _parser   = new MockCommandParser();
//             _history  = new MockCommandHistory();
//             _sut = new ExecuteCommandUseCase(
//                 _logger, _registry, _invoker, _parser, _history);
//         }
//
//         // ─── 正常系: コマンド実行 ────────────────────────────────────────────
//
//         [Test]
//         public async Task ExecuteAsync_String_ValidCommand_InvokesHandler()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help");
//
//             Assert.IsTrue(_invoker.WasCalled);
//         }
//
//         [Test]
//         public async Task ExecuteAsync_Memory_ValidCommand_InvokesHandler()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help".AsMemory());
//
//             Assert.IsTrue(_invoker.WasCalled);
//         }
//
//         [Test]
//         public async Task ExecuteAsync_ValidCommand_AddsToHistory()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help");
//
//             Assert.AreEqual(1, _history.Histories.Count);
//             CollectionAssert.Contains(_history.Histories, "help");
//         }
//
//         [Test]
//         public async Task ExecuteAsync_ValidCommand_LogsEntry()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help");
//
//             Assert.IsTrue(_logger.Sent.Count > 0);
//             Assert.AreEqual(MessageType.Entry, _logger.Sent[0].type);
//         }
//
//         // ─── 正常系: 空/null 入力 ─────────────────────────────────────────────
//
//         [Test]
//         public async Task ExecuteAsync_EmptyCommand_DoesNotInvoke()
//         {
//             _parser.CommandName = null;
//             _parser.StatusCode  = ICommandParser.ParseStatusCode.MalformedInput;
//
//             await _sut.ExecuteAsync(string.Empty);
//
//             Assert.IsFalse(_invoker.WasCalled);
//         }
//
//         [Test]
//         public async Task ExecuteAsync_NullString_DoesNotInvoke()
//         {
//             _parser.CommandName = null;
//
//             await _sut.ExecuteAsync((string)null);
//
//             Assert.IsFalse(_invoker.WasCalled);
//         }
//
//         // ─── 異常系: 未登録コマンド ──────────────────────────────────────────
//
//         [Test]
//         public async Task ExecuteAsync_UnknownCommand_LogsError()
//         {
//             _parser.CommandName = "unknown";
//
//             await _sut.ExecuteAsync("unknown");
//
//             Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_UnknownCommand_DoesNotInvoke()
//         {
//             _parser.CommandName = "unknown";
//
//             await _sut.ExecuteAsync("unknown");
//
//             Assert.IsFalse(_invoker.WasCalled);
//         }
//
//         [Test]
//         public async Task ExecuteAsync_UnknownCommand_ErrorMessageContainsCommandName()
//         {
//             _parser.CommandName = "badcmd";
//
//             await _sut.ExecuteAsync("badcmd");
//
//             Assert.IsTrue(_logger.Sent.Exists(s =>
//                 s.type == MessageType.Error && s.message.Contains("badcmd")));
//         }
//
//         // ─── 異常系: SyntaxError ─────────────────────────────────────────────
//
//         [Test]
//         public async Task ExecuteAsync_SyntaxError_LogsError()
//         {
//             _parser.CommandName = "help";
//             _parser.StatusCode  = ICommandParser.ParseStatusCode.SyntaxError;
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help");
//
//             Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_SyntaxError_DoesNotInvoke()
//         {
//             _parser.CommandName = "help";
//             _parser.StatusCode  = ICommandParser.ParseStatusCode.SyntaxError;
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help");
//
//             Assert.IsFalse(_invoker.WasCalled);
//         }
//
//         // ─── 異常系: 例外ハンドリング ────────────────────────────────────────
//
//         [Test]
//         public async Task ExecuteAsync_CommandArgumentException_LogsException()
//         {
//             _parser.CommandName    = "cmd";
//             _registry.Add("cmd", default);
//             _invoker.ThrowException = new CommandArgumentException(3, 1, 2, null);
//
//             await _sut.ExecuteAsync("cmd");
//
//             Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_CommandFormatException_LogsException()
//         {
//             _parser.CommandName    = "cmd";
//             _registry.Add("cmd", default);
//             _invoker.ThrowException = new CommandFormatException(0, "abc", typeof(int), null);
//
//             await _sut.ExecuteAsync("cmd");
//
//             Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_UnexpectedException_LogsException()
//         {
//             _parser.CommandName    = "cmd";
//             _registry.Add("cmd", default);
//             _invoker.ThrowException = new InvalidOperationException("unexpected");
//
//             await _sut.ExecuteAsync("cmd");
//
//             Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_UnexpectedException_ExceptionTypeInMessage()
//         {
//             _parser.CommandName    = "cmd";
//             _registry.Add("cmd", default);
//             _invoker.ThrowException = new InvalidOperationException("oops");
//
//             await _sut.ExecuteAsync("cmd");
//
//             Assert.IsTrue(_logger.Sent.Exists(s =>
//                 s.type == MessageType.Exception &&
//                 s.message.Contains(nameof(InvalidOperationException))));
//         }
//
//         // ─── CancellationToken ────────────────────────────────────────────────
//
//         [Test]
//         public void ExecuteAsync_String_CancelledToken_ThrowsOperationCanceledException()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//             var cts = new CancellationTokenSource();
//             cts.Cancel();
//
//             Assert.ThrowsAsync<OperationCanceledException>(
//                 async () => await _sut.ExecuteAsync("help", cts.Token));
//         }
//
//         [Test]
//         public void ExecuteAsync_Memory_CancelledToken_ThrowsOperationCanceledException()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//             var cts = new CancellationTokenSource();
//             cts.Cancel();
//
//             Assert.ThrowsAsync<OperationCanceledException>(
//                 async () => await _sut.ExecuteAsync("help".AsMemory(), cts.Token));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_ActiveToken_ExecutesNormally()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//
//             await _sut.ExecuteAsync("help", CancellationToken.None);
//
//             Assert.IsTrue(_invoker.WasCalled);
//         }
//
//         [Test]
//         public async Task ExecuteAsync_CancelledToken_DoesNotInvoke()
//         {
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//             var cts = new CancellationTokenSource();
//             cts.Cancel();
//
//             try { await _sut.ExecuteAsync("help", cts.Token); }
//             catch (OperationCanceledException) { }
//
//             Assert.IsFalse(_invoker.WasCalled);
//         }
//
//         // ─── null logger 許容 ────────────────────────────────────────────────
//
//         [Test]
//         public async Task ExecuteAsync_NullLogger_ValidCommand_DoesNotThrow()
//         {
//             var sut = new ExecuteCommandUseCase(
//                 null, _registry, _invoker, _parser, _history);
//             _parser.CommandName = "help";
//             _registry.Add("help", default);
//
//             Assert.DoesNotThrowAsync(async () => await sut.ExecuteAsync("help"));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_NullLogger_UnknownCommand_DoesNotThrow()
//         {
//             var sut = new ExecuteCommandUseCase(
//                 null, _registry, _invoker, _parser, _history);
//             _parser.CommandName = "unknown";
//
//             Assert.DoesNotThrowAsync(async () => await sut.ExecuteAsync("unknown"));
//         }
//
//         [Test]
//         public async Task ExecuteAsync_NullLogger_Exception_DoesNotThrow()
//         {
//             var sut = new ExecuteCommandUseCase(
//                 null, _registry, _invoker, _parser, _history);
//             _parser.CommandName     = "cmd";
//             _registry.Add("cmd", default);
//             _invoker.ThrowException = new InvalidOperationException("err");
//
//             Assert.DoesNotThrowAsync(async () => await sut.ExecuteAsync("cmd"));
//         }
//     }
// }

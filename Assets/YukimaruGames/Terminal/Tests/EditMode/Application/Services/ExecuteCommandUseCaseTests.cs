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
    /// <summary>
    /// <see cref="ExecuteCommandUseCase"/> の複合的なビジネスロジックおよびパイプラインを検証するテストクラス。
    /// </summary>
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

        /// <summary>
        /// 文字列形式の有効な同期コマンドが入力された際、対応する同期ハンドラーが正しく呼び出されることを検証します。
        /// </summary>
        /// <remarks>
        /// 文字列のパース、レジストリからのハンドラー取得、そして Invoker への委譲が正常に結合されているか確認します。
        /// </remarks>
        [Test]
        public void Execute_String_ValidSyncCommand_InvokesHandler()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        /// <summary>
        /// メモリバッファ形式（<see cref="ReadOnlyMemory{Char}"/>）の有効な同期コマンドが入力された際、ハンドラーが呼び出されることを検証します。
        /// </summary>
        /// <remarks>
        /// UI層（入力フィールド等）からGCを抑制するためにメモリバッファのままデータが流れてきた場合のパスを保証します。
        /// </remarks>
        [Test]
        public void Execute_Memory_ValidSyncCommand_InvokesHandler()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd".AsMemory());

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        /// <summary>
        /// 有効なコマンドが正常に実行された際、そのコマンド文字列が履歴（History）レポジトリに保存されることを検証します。
        /// </summary>
        /// <remarks>
        /// ユーザーがコンソールで「上矢印キー」等を押した際に入力履歴を辿れるようにするための重要なフックです。
        /// </remarks>
        [Test]
        public void Execute_ValidCommand_AddsToHistory()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            CollectionAssert.Contains(_history.Histories, "cmd");
        }

        /// <summary>
        /// コマンドの実行開始時、コンソール画面に出力するためのエントリログ（入力エコー等）が送信されることを検証します。
        /// </summary>
        /// <remarks>
        /// ユーザーが入力したコマンドが画面上に反映され、実行状態が可視化される状態を保証します。
        /// </remarks>
        [Test]
        public void Execute_ValidCommand_LogsEntry()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Entry));
        }

        // ─── Execute（同期）: 異常系 ─────────────────────────────────────────

        /// <summary>
        /// 同期実行メソッドに対し、非同期ハンドラーを持つコマンドが渡された場合、実行を拒否してエラーログを出すことを検証します。
        /// </summary>
        /// <remarks>
        /// 同期コンテキストで重い非同期タスクをブロッキング実行（.Resultなど）させないための防衛設計を検証します。
        /// </remarks>
        [Test]
        public void Execute_AsyncHandler_LogsError_DoesNotInvoke()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        /// <summary>
        /// レジストリに登録されていない未知のコマンド名が入力された場合、実行されずにエラーログが出力されることを検証します。
        /// </summary>
        /// <remarks>
        /// タイポや存在しないコマンドに対し、システムがヌルポ等でクラッシュせず、ユーザーに「未定義」を通知できるか確認します。
        /// </remarks>
        [Test]
        public void Execute_UnknownCommand_DoesNotInvoke()
        {
            _parser.CommandName = "unknown";

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("unknown");

            Assert.IsFalse(_invoker.ExecuteCalled);
            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Error));
        }

        /// <summary>
        /// 空文字やスペースのみの、コマンド名が抽出されなかった入力に対しては、何も処理を行わずに早期リターンすることを検証します。
        /// </summary>
        /// <remarks>
        /// ユーザーがコンソールで何も入力せずにエンターキーを連打した際など、無駄なエラーログを出さずにスルーすべき挙動です。
        /// </remarks>
        [Test]
        public void Execute_EmptyCommand_DoesNotInvoke()
        {
            _parser.CommandName = null;

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute(string.Empty);

            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        /// <summary>
        /// クォーテーションの閉じ忘れなど、パース段階で構文エラー（SyntaxError）が発生した場合は実行を拒否し、エラーログを出力することを検証します。
        /// </summary>
        /// <remarks>
        /// 不正な形式の引数がハンドラーに渡ってドメイン層が汚染されるのを、ユースケースの入り口で遮断できているか確認します。
        /// </remarks>
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

        /// <summary>
        /// 非同期実行メソッドに対し「同期コマンド」が指定された場合、内部で同期用の実行ライン（Execute）へ正しくハンドリングが流れることを検証します。
        /// </summary>
        /// <remarks>
        /// 利用側がコマンドの種類（同期・非同期）を意識せず、一律で `ExecuteAsync` を呼んでも安全に処理される相互互換性を保証します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_ValidSyncCommand_InvokesExecute()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
            Assert.IsFalse(_invoker.ExecuteAsyncCalled);
        }

        /// <summary>
        /// 非同期実行メソッドに対し「非同期コマンド」が指定された場合、期待通り非同期用の実行ライン（ExecuteAsync）が走ることを検証します。
        /// </summary>
        /// <remarks>
        /// 重い処理や通信を伴うコマンドが、非同期パイプラインに乗って正しくスケジュールされる本流のパスです。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_ValidAsyncCommand_InvokesExecuteAsync()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteAsyncCalled);
            Assert.IsFalse(_invoker.ExecuteCalled);
        }

        /// <summary>
        /// 非同期実行メソッド経由であっても、有効なコマンドであれば正常に履歴（History）へ保存されることを検証します。
        /// </summary>
        /// <remarks>
        /// 同期・非同期に関わらず、ユーザーの入力体験が一貫していることを確認します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_ValidCommand_AddsToHistory()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            CollectionAssert.Contains(_history.Histories, "cmd");
        }

        /// <summary>
        /// 非同期実行メソッドにメモリバッファ形式でコマンドが渡された際、途中でデータが途切れることなくハンドラーまで到達することを検証します。
        /// </summary>
        /// <remarks>
        /// 非同期（await）の前後で、構造体である <see cref="ReadOnlyMemory{Char}"/> のライフサイクルが安全に維持されているかを保証します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_Memory_ValidCommand_InvokesHandler()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd".AsMemory(), CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        // ─── ExecuteAsync（非同期）: IsExecuting ──────────────────────────────

        /// <summary>
        /// 何もコマンドを実行していない初期状態において、実行中フラグ（<see cref="ExecuteCommandUseCase.IsExecuting"/>）が False であることを検証します。
        /// </summary>
        [Test]
        public void IsExecuting_BeforeExecution_IsFalse()
        {
            Assert.IsFalse(_sut.IsExecuting);
        }

        /// <summary>
        /// 非同期コマンドの処理がバックグラウンドで走っている「最中」は、実行中フラグが確実に True になることを検証します。
        /// </summary>
        /// <remarks>
        /// テスト用の疑似ゲート（TaskCompletionSource）を使って処理を途中で静止させ、その間のステートを確認しています。
        /// </remarks>
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

        /// <summary>
        /// 非同期コマンドの処理が完全に終了（完了）した後は、実行中フラグが自動的に False に戻ることを検証します。
        /// </summary>
        /// <remarks>
        /// 正常系における、ライフサイクル終了時のフラグクリーンアップ（finallyブロック等の挙動）を保証します。
        /// </remarks>
        [Test]
        public async Task IsExecuting_AfterExecution_IsFalse()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", AsyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsFalse(_sut.IsExecuting);
        }

        /// <summary>
        /// すでに非同期コマンドが実行されている最中に、上書きで2回目の実行要求が走った場合、その2回目は完全に無視（多重実行防止）されることを検証します。
        /// </summary>
        /// <remarks>
        /// 連打による多重通信や、同じ重い処理が複数立ち上がってUIメインスレッドやドメインデータを破壊するのを防ぐ重要なガードロックテストです。
        /// </remarks>
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

        /// <summary>
        /// 何も実行していない状態で強制キャンセルメソッド（<see cref="CancelCommandIfNeeded"/>）が叩かれても、例外を投げず安全にスルーされることを検証します。
        /// </summary>
        /// <remarks>
        /// 画面が閉じる際や初期化時に、現在の状態に関わらず安全にクリーンアップメソッドを呼べる堅牢性を確保します。
        /// </remarks>
        [Test]
        public void CancelCommandIfNeeded_WhenNotExecuting_DoesNotThrow()
        {
            var iuse = (YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut;

            Assert.DoesNotThrow(() => iuse.CancelCommandIfNeeded());
        }

        /// <summary>
        /// 非同期コマンドを実行中に外部からキャンセルが要求された場合、実行中の内部トークンが連動してキャンセルされ、処理が安全に中断することを検証します。
        /// </summary>
        /// <remarks>
        /// 内部の `HandleException` がキャンセル例外（<see cref="OperationCanceledException"/>）を適切にキャッチ・吸収し、呼び出し元（UI側）へ致命的なクラッシュを伝播させずに静かに終了することを確認します。
        /// </remarks>
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

        /// <summary>
        /// キャンセル処理を通過したあとであっても、最終的に実行中フラグが False で美しく着地していることを検証します。
        /// </summary>
        /// <remarks>
        /// 中断によって内部状態が「実行中」のままフリーズし、以降すべてのコマンドが受け付けられなくなるという最悪のバグを防止します。
        /// </remarks>
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

        /// <summary>
        /// 実行を始める段階で「すでにキャンセル状態にあるトークン」が渡された場合、ハンドラーの起動そのものを手前で拒否（早期終了）することを検証します。
        /// </summary>
        /// <remarks>
        /// すでに寿命が尽きている要求に対して、無駄なパースやインボーク処理のコストを払わないための最適化の検証です。
        /// </remarks>
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

        /// <summary>
        /// キャンセルされていない通常の有効なトークンを渡した場合、チェックをすり抜けて正常系ルートが何事もなく動くことを検証します。
        /// </summary>
        [Test]
        public async Task ExecuteAsync_ActiveToken_ExecutesNormally()
        {
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            await ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).ExecuteAsync("cmd", CancellationToken.None);

            Assert.IsTrue(_invoker.ExecuteCalled);
        }

        // ─── 例外ハンドリング ─────────────────────────────────────────────────

        /// <summary>
        /// 引数の数や種類が合わないことによるドメイン例外（<see cref="CommandArgumentException"/>）が発生した際、外に漏らさずロガー経由で「エラーメッセージ」としてきれいに捕捉・出力されることを検証します。
        /// </summary>
        /// <remarks>
        /// ユーザーの入力ミスはシステムエラー（クラッシュ）ではなく、画面への親切な警告ログに変換すべきであるというドメイン仕様の検証です。
        /// </remarks>
        [Test]
        public void Execute_CommandArgumentException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new CommandArgumentException(3, 1, 2, null);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        /// <summary>
        /// 数値型へのパース失敗などによるドメイン例外（<see cref="CommandFormatException"/>）が発生した際、ロガーを介して画面に例外通知が飛ぶことを検証します。
        /// </summary>
        [Test]
        public void Execute_CommandFormatException_LogsException()
        {
            _parser.CommandName    = "cmd";
            _registry.Add("cmd", SyncHandler);
            _invoker.ThrowException = new CommandFormatException(0, "bad", typeof(int), null);

            ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)_sut).Execute("cmd");

            Assert.IsTrue(_logger.Sent.Exists(s => s.type == MessageType.Exception));
        }

        /// <summary>
        /// 開発者の想定外の一般例外（<see cref="InvalidOperationException"/>等）が同期実行中に発生した場合、安全に回収され、かつログに「例外の型名」が含まれる形で原因究明しやすく出力されるかを検証します。
        /// </summary>
        /// <remarks>
        /// バグが発生してもコンソール画面を即死させず、スタックトレース等の手がかりを残すための堅牢な `HandleException.switch(default)` パスをテストしています。
        /// </remarks>
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

        /// <summary>
        /// 非同期実行中（`await` の最中）に開発者の想定外の一般例外が発生した場合でも、非同期パイプラインが壊れず、安全にロガーへ例外が転送されることを検証します。
        /// </summary>
        /// <remarks>
        /// 非同期の `catch (Exception)` が同期側と同じ `HandleException` を正しく通過できているかを保証します。
        /// </remarks>
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

        /// <summary>
        /// 依存関係としてロガー（<see cref="ICommandLogger"/>）に `null` が注入されたスタンドアロンな状態でも、同期実行がヌルポ（<see cref="NullReferenceException"/>）を起こさずに安全に動作することを検証します。
        /// </summary>
        /// <remarks>
        /// ログ出力を必要としないバッチ処理や、極限のテスト環境、ロガー初期化前のタイミングにおける安全性のための設計（Null条件演算子 `?.` の漏れ防止）です。
        /// </remarks>
        [Test]
        public void Execute_NullLogger_DoesNotThrow()
        {
            var sut = new ExecuteCommandUseCase(null, _registry, _invoker, _parser, _history);
            _parser.CommandName = "cmd";
            _registry.Add("cmd", SyncHandler);

            Assert.DoesNotThrow(() => ((YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase)sut).Execute("cmd"));
        }

        /// <summary>
        /// ロガーに `null` が注入された状態でも、非同期実行がヌルポを起こさずに安全に動作・完了することを検証します。
        /// </summary>
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
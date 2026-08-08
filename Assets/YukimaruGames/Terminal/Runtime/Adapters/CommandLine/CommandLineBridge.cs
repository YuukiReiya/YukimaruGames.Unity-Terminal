using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Models;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// 127.0.0.1のループバックTCPソケットを介して、外部ターミナルプロセス(cmd.exe/zsh等で
    /// 動く中継スクリプト)と<see cref="ITerminalService"/>との間で1行単位の入出力を中継するブリッジ.
    /// </summary>
    /// <remarks>
    /// 標準入出力のリダイレクトでは外部ターミナルのウィンドウに何も表示されないため
    /// (子プロセスのコンソールバッファへの出力自体が止まるため)、ソケット越しに
    /// テキスト行を送受信し、外部ターミナル側の中継スクリプトがコンソールへ描画する構成を取る.
    /// </remarks>
    public sealed class CommandLineBridge : IDisposable
    {
        /// <summary>
        /// プロンプト行(キャレット代わり)の目印文字列.
        /// </summary>
        /// <remarks>
        /// 通常のログ行(<see cref="HandleLogAdded"/>)は末尾に改行を付けて送るのに対し、
        /// プロンプトは「同じ行の続きにユーザーの入力が来る」ことを期待するため改行を付けない。
        /// ただし中継スクリプト側の受信ループは改行区切りで1行ずつ読む都合上、プロンプト自体は
        /// 改行付きの1行として送りつつ、この目印を先頭に付けることで中継スクリプト側に
        /// 「改行せず出力しろ」と伝える(<see cref="CommandLineRelayScriptWriter"/>の受信ループ実装を参照).
        ///
        /// なお、この目印はそのままでは使わず、必ず<see cref="Token"/>を前置した
        /// 「<c>&lt;token&gt;PROMPT</c>」という形で送出する(<see cref="ControlLine"/>)。
        /// 素の"PROMPT"のままだと、コマンドの実行結果が偶然その文字列で始まった場合
        /// (例: <c>echo PROMPTing works</c>)に中継スクリプトが制御行と誤認し、
        /// 本来表示すべき出力が消えてしまうため。トークンはセッション毎の乱数であり
        /// 外部から予測できないので、通常の出力が偶然一致することはない.
        /// </remarks>
        private const string PromptSentinel = "PROMPT";

        /// <summary>
        /// クライアントからの自動補完リクエストの目印(この文字列で始まる入力行は
        /// コマンドとして実行せず、<see cref="ITerminalService.Autocomplete"/>への
        /// 問い合わせとして扱う).
        /// </summary>
        private const string AutocompleteRequestPrefix = "AUTOCOMPLETE:";

        /// <summary>
        /// 自動補完の結果、候補が1件だけに絞れた場合の応答の目印(直後に補完後の
        /// 文字列が続く。中継スクリプトはこれで現在の入力行を置き換える).
        /// </summary>
        /// <remarks>
        /// <see cref="PromptSentinel"/>と同様、送出時は<see cref="Token"/>を前置する.
        /// </remarks>
        private const string AutocompleteCompleteResponsePrefix = "COMPLETE:";

        /// <summary>
        /// 自動補完の結果、候補が複数あった場合の応答の目印(直後に空白区切りの
        /// 候補一覧が続く。中継スクリプトはこれを新しい行として表示する).
        /// </summary>
        /// <remarks>
        /// <see cref="PromptSentinel"/>と同様、送出時は<see cref="Token"/>を前置する.
        /// </remarks>
        private const string AutocompleteCandidatesResponsePrefix = "CANDIDATES:";

        /// <summary>
        /// 自動補完の結果、候補が無かった場合の応答.
        /// </summary>
        /// <remarks>
        /// <see cref="PromptSentinel"/>と同様、送出時は<see cref="Token"/>を前置する.
        /// </remarks>
        private const string AutocompleteNoMatchResponse = "NOMATCH";

        /// <summary>
        /// セッショントークンのバイト長(16バイト=128bit。総当たりで当てられる強度ではない).
        /// </summary>
        private const int TokenByteLength = 16;

        /// <summary>
        /// 接続確立からトークン行が届くまでの猶予(ミリ秒).
        /// </summary>
        /// <remarks>
        /// 接続だけして何も送らないクライアントがソケットを掴んだまま滞留するのを防ぐ.
        /// </remarks>
        private const int HandshakeTimeoutMilliseconds = 10000;

        /// <summary>
        /// ANSIエスケープシーケンスによる画面クリア(カーソルを左上へ戻す).
        /// </summary>
        /// <remarks>
        /// 中継スクリプト(bash/PowerShell)側の受信ループは改行区切りで1行ずつ読むため、
        /// 末尾の"\n"が無いとバッファに滞留し、次の(改行付きの)出力が来るまで反映されない
        /// (=クリアが1テンポ遅れて見える不具合の原因だった).
        /// </remarks>
        private static readonly byte[] AnsiClearScreen = Encoding.UTF8.GetBytes("\x1b[2J\x1b[H\n");

        private readonly ITerminalService _service;
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// 認証済み(トークン照合を通過した)クライアント。ログ・プロンプトの配信先.
        /// </summary>
        private readonly List<TcpClient> _clients = new();

        /// <summary>
        /// 接続は受け付けたがまだ認証が済んでいないクライアント.
        /// </summary>
        /// <remarks>
        /// <see cref="_clients"/>に入る前の接続も<see cref="Dispose"/>で確実に閉じるために保持する
        /// (閉じないと、ソケットの読み取り待ちが解けずに接続がリークし続ける).
        /// </remarks>
        private readonly List<TcpClient> _pendingClients = new();

        private readonly object _clientsLock = new();
        private readonly SynchronizationContext _mainThreadContext;
        private bool _disposed;

        /// <summary>
        /// 割り当てられたループバックポート番号.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// このセッション固有のランダムなトークン(16進文字列).
        /// </summary>
        /// <remarks>
        /// ループバックのポート番号は秘密情報ではなく、同一マシンの任意のプロセスが
        /// 127.0.0.1を走査すれば到達できてしまう。無認証のままだとログの盗み見や
        /// <see cref="ITerminalService.ExecuteAsync"/>経由の任意コマンド実行を許すため、
        /// 接続後の最初の1行がこのトークンと一致した接続のみを受け入れる。
        /// 併せて、サーバーからの制御行(プロンプト・自動補完応答)の目印にも前置し、
        /// 通常の出力行が制御行と誤認される事故を防ぐ役割も持たせている.
        /// </remarks>
        public string Token { get; }

        public CommandLineBridge(ITerminalService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Token = GenerateToken();

            // Installer.Install()はUnityのAwake()から呼ばれる(=Unityメインスレッド)前提のため、
            // ここで取得できるコンテキストは常にUnityメインスレッドのものになる。
            // ITerminalService(CommandLoggerのQueue<T>含む)はスレッドセーフではなく、IMGUI側は常に
            // メインスレッドからしか呼ばれてこなかったため問題が表面化していなかった。このBridgeは
            // ソケットの受信スレッド(バックグラウンド)から呼び出す初めての経路になるため、
            // コマンド実行は必ずこのコンテキスト経由でメインスレッドへ戻して行う.
            _mainThreadContext = SynchronizationContext.Current;

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _service.OnLogAdded += HandleLogAdded;
            _service.OnLogRemoved += HandleLogRemoved;

            _ = AcceptLoopAsync(_cts.Token);
        }

        /// <summary>
        /// 制御行(プロンプト・自動補完応答)を組み立てる.
        /// </summary>
        /// <remarks>
        /// 目印の前に必ず<see cref="Token"/>を置くことで、通常のログ行(前置しない)と
        /// 確実に区別できるようにする(<see cref="PromptSentinel"/>のremarks参照).
        /// </remarks>
        private string ControlLine(string sentinel, string body = "") => Token + sentinel + body;

        /// <summary>
        /// 暗号論的乱数からセッショントークンを生成する(16進文字列).
        /// </summary>
        /// <remarks>
        /// Unityのランタイム(Mono/IL2CPP)ではバージョンによって<c>Convert.ToHexString</c>等の
        /// 新しめのAPIが存在しないため、<see cref="RandomNumberGenerator"/>と手書きの16進変換という
        /// 最も互換性の高い組み合わせで実装している.
        /// </remarks>
        private static string GenerateToken()
        {
            var bytes = new byte[TokenByteLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

                    lock (_clientsLock)
                    {
                        if (_disposed)
                        {
                            // Dispose()は_disposedを立ててからこのロックを取るため、ここで
                            // 弾いておかないと「Dispose()の後始末より後に追加された接続」が
                            // 誰にも閉じられずに残る.
                            client.Close();
                            continue;
                        }

                        _pendingClients.Add(client);
                    }

                    // 認証が終わるまではログもプロンプトも一切送らない。
                    // また、認証待ちで接続がここをブロックしないよう、待たずに次のAcceptへ戻る.
                    _ = HandleClientAsync(client, ct);
                }
            }
            catch (ObjectDisposedException)
            {
                // Dispose()によるリスナー停止時の正常系.
            }
            catch (SocketException)
            {
                // リスナー停止時の正常系.
            }
        }

        /// <summary>
        /// 接続を受け付けたクライアントを認証し、通過した場合のみ配信対象に加えて
        /// コマンド受信ループへ進む.
        /// </summary>
        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    if (!await AuthenticateAsync(reader, ct).ConfigureAwait(false))
                    {
                        // トークンが一致しない(あるいは時間内に届かない)接続は、
                        // 何も送らず何も実行せずに即座に閉じる.
                        return;
                    }

                    lock (_clientsLock)
                    {
                        _pendingClients.Remove(client);
                        _clients.Add(client);
                    }

                    SendPromptTo(client);
                    await ClientReadLoopAsync(client, reader, ct).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // クライアント切断時の正常系.
            }
            catch (ObjectDisposedException)
            {
                // Dispose()時の正常系.
            }
            catch (InvalidOperationException)
            {
                // 接続直後に切断された場合、GetStream()が投げる.
            }
            catch (SocketException)
            {
                // クライアント切断時の正常系.
            }
            finally
            {
                lock (_clientsLock)
                {
                    _pendingClients.Remove(client);
                    _clients.Remove(client);
                }
            }
        }

        /// <summary>
        /// クライアントが最初に送ってくる1行が<see cref="Token"/>と一致するかを確認する.
        /// </summary>
        private async Task<bool> AuthenticateAsync(StreamReader reader, CancellationToken ct)
        {
            var readTask = reader.ReadLineAsync();

            // ReadLineAsync自体はキャンセルできないため、猶予時間(とDispose)で打ち切る。
            // 打ち切った場合、呼び出し元のusingがクライアントを閉じることで読み取りも終了する.
            var completed = await Task.WhenAny(readTask, Task.Delay(HandshakeTimeoutMilliseconds, ct))
                .ConfigureAwait(false);

            if (completed != readTask)
            {
                ObserveFault(readTask);
                return false;
            }

            var line = await readTask.ConfigureAwait(false);
            return FixedTimeEquals(line, Token);
        }

        /// <summary>
        /// 打ち切った読み取りタスクの例外を観測済みにする
        /// (未観測のTask例外がファイナライズ時に表面化するのを避けるため).
        /// </summary>
        private static void ObserveFault(Task task) =>
            task.ContinueWith(
                t => { _ = t.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

        /// <summary>
        /// トークン照合を、一致する先頭文字数によって処理時間が変わらない形で行う
        /// (タイミング差からトークンを1文字ずつ絞り込まれるのを防ぐ).
        /// </summary>
        /// <remarks>
        /// <c>CryptographicOperations.FixedTimeEquals</c>はUnityのランタイムに存在しない
        /// 場合があるため、自前で実装している.
        /// </remarks>
        private static bool FixedTimeEquals(string actual, string expected)
        {
            if (actual == null || expected == null || actual.Length != expected.Length)
            {
                return false;
            }

            var difference = 0;
            for (var i = 0; i < actual.Length; i++)
            {
                difference |= actual[i] ^ expected[i];
            }

            return difference == 0;
        }

        private async Task ClientReadLoopAsync(TcpClient client, StreamReader reader, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                {
                    break;
                }

                if (line.StartsWith(AutocompleteRequestPrefix, StringComparison.Ordinal))
                {
                    var partialWord = line.Substring(AutocompleteRequestPrefix.Length);
                    await RespondToAutocompleteAsync(client, partialWord, ct).ConfigureAwait(false);
                    continue;
                }

                await ExecuteOnMainThreadAsync(line, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// コマンド実行(とその失敗時のログ出力)をUnityメインスレッドへポストし、完了を待つ.
        /// 例外はここで全てログへ変換して握りつぶすため、呼び出し元(バックグラウンドスレッド)へは
        /// 伝播しない(=ソケットを閉じてしまう誤動作を防ぐ).
        /// </summary>
        /// <remarks>
        /// Play終了/Dispose()等でUnity側のメッセージポンプが停止すると、Postしたデリゲートが
        /// 二度と実行されず<paramref name="ct"/>だけが唯一の抜け道になる場合がある。
        /// ctのキャンセルでも完了するようにし、ClientReadLoopAsyncのawaitが永久に返らない
        /// (=接続がリークし続ける)事態を避ける.
        /// </remarks>
        private async Task ExecuteOnMainThreadAsync(string line, CancellationToken ct)
        {
            if (_mainThreadContext == null)
            {
                // メインスレッドのコンテキストを取得できなかった場合のフォールバック
                // (本来は起こらない想定だが、呼び出し元を巻き込まないようその場で処理する).
                await ExecuteAndLogAsync(line, ct).ConfigureAwait(false);
                return;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = ct.Register(() => tcs.TrySetResult(true));

            _mainThreadContext.Post(async _ =>
            {
                try
                {
                    await ExecuteAndLogAsync(line, ct).ConfigureAwait(true);
                }
                finally
                {
                    tcs.TrySetResult(true);
                }
            }, null);

            await tcs.Task.ConfigureAwait(false);
        }

        private async Task ExecuteAndLogAsync(string line, CancellationToken ct)
        {
            try
            {
                await _service.ExecuteAsync(line, ct).ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Dispose()に伴うキャンセルの正常系.
            }
            catch (Exception e)
            {
                _service.Exception(e.ToString());
            }
            finally
            {
                // コマンド完了後、実行結果のログ出力(HandleLogAdded)より後にプロンプトを送ることで
                // 「出力の下に次のプロンプトが来る」通常のシェルの見た目に合わせる.
                SendPromptToAll();
            }
        }

        /// <summary>
        /// 自動補完リクエストに応答する(IMGUI版のTabキー相当の機能を外部ターミナルにも提供する).
        /// <see cref="ITerminalService"/>への問い合わせはメインスレッドへ寄せた上で行う.
        /// </summary>
        /// <remarks>
        /// <see cref="ExecuteOnMainThreadAsync"/>と同様、Play終了/Dispose()等でメッセージポンプが
        /// 止まった場合に備え、<paramref name="ct"/>のキャンセルでも完了するようにしている.
        /// </remarks>
        private async Task RespondToAutocompleteAsync(TcpClient client, string partialWord, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = ct.Register(() => tcs.TrySetResult(true));

            RunOnMainThread(() =>
            {
                try
                {
                    string[] results;
                    try
                    {
                        results = _service.Autocomplete(partialWord) ?? Array.Empty<string>();
                    }
                    catch (Exception e)
                    {
                        _service.Exception(e.ToString());
                        results = Array.Empty<string>();
                    }

                    string response = results.Length switch
                    {
                        0 => ControlLine(AutocompleteNoMatchResponse),
                        1 => ControlLine(AutocompleteCompleteResponsePrefix, results[0]),
                        _ => ControlLine(AutocompleteCandidatesResponsePrefix, string.Join(" ", results)),
                    };

                    TryWrite(client, Encoding.UTF8.GetBytes(response + "\n"));
                }
                finally
                {
                    tcs.TrySetResult(true);
                }
            });

            await tcs.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// 新規接続したクライアントへ、キャレット代わりのプロンプトを送る(接続直後は
        /// まだ何も入力されておらず、入力可能であることが視覚的にわからないため).
        /// </summary>
        private void SendPromptTo(TcpClient client)
        {
            RunOnMainThread(() =>
            {
                var bytes = Encoding.UTF8.GetBytes(ControlLine(PromptSentinel, _service.Prompt) + "\n");
                TryWrite(client, bytes);
            });
        }

        /// <summary>
        /// 接続中の全クライアントへ、キャレット代わりのプロンプトを送る.
        /// </summary>
        /// <remarks>
        /// 呼び出し元(<see cref="ExecuteAndLogAsync"/>の<c>finally</c>)は既にUnityメインスレッド上で
        /// 実行されているため、ここでは<see cref="RunOnMainThread"/>を経由しない.
        /// </remarks>
        private void SendPromptToAll()
        {
            List<TcpClient> snapshot;
            lock (_clientsLock)
            {
                if (_clients.Count == 0)
                {
                    return;
                }

                snapshot = new List<TcpClient>(_clients);
            }

            var bytes = Encoding.UTF8.GetBytes(ControlLine(PromptSentinel, _service.Prompt) + "\n");
            foreach (var client in snapshot)
            {
                TryWrite(client, bytes);
            }
        }

        /// <summary>
        /// <see cref="ITerminalService"/>への読み取りアクセスもメインスレッドへ寄せるための
        /// 汎用ヘルパー(<see cref="ExecuteOnMainThreadAsync"/>と異なり完了を待たない).
        /// </summary>
        private void RunOnMainThread(Action action)
        {
            if (_mainThreadContext == null)
            {
                action();
                return;
            }

            _mainThreadContext.Post(_ => action(), null);
        }

        /// <summary>
        /// ログバッファの全件削除(clearコマンド等による<c>ICommandLogger.Clear()</c>)を検知し、
        /// 外部ターミナル側の画面もクリアする.
        /// </summary>
        /// <remarks>
        /// 外部ターミナルは「新しい行を都度echoする」だけの追記型プロトコルのため、
        /// サーバー側のログバッファを空にしても、外部ターミナル側に既に印字済みの文字は
        /// 消えずに残ってしまう。全件削除時のみ(=削除後に<see cref="ITerminalService.Logs"/>が
        /// 空になった場合のみ)ANSIの画面クリアシーケンスを送ることで、部分的なリングバッファの
        /// エビクション(古い1件だけ削除される通常の追加時の動作)と区別する.
        /// </remarks>
        private void HandleLogRemoved(LogEntry[] entries)
        {
            if (entries == null || entries.Length == 0 || _service.Logs.Count > 0)
            {
                return;
            }

            List<TcpClient> snapshot;
            lock (_clientsLock)
            {
                if (_clients.Count == 0)
                {
                    return;
                }

                snapshot = new List<TcpClient>(_clients);
            }

            foreach (var client in snapshot)
            {
                TryWrite(client, AnsiClearScreen);
            }
        }

        private void HandleLogAdded(LogEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                return;
            }

            List<TcpClient> snapshot;
            lock (_clientsLock)
            {
                if (_clients.Count == 0)
                {
                    return;
                }

                snapshot = new List<TcpClient>(_clients);
            }

            foreach (var entry in entries)
            {
                var bytes = Encoding.UTF8.GetBytes(FormatLine(entry) + "\n");

                foreach (var client in snapshot)
                {
                    TryWrite(client, bytes);
                }
            }
        }

        private static void TryWrite(TcpClient client, byte[] bytes)
        {
            try
            {
                if (!client.Connected)
                {
                    return;
                }

                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (IOException)
            {
                // 切断済みクライアントへの書き込み失敗は無視する(次のReadLoopで除去される).
            }
            catch (ObjectDisposedException)
            {
                // 切断済みクライアントへの書き込み失敗は無視する.
            }
            catch (InvalidOperationException)
            {
                // client.Connectedのチェック直後に切断された場合、GetStream()が投げる.
            }
            catch (SocketException)
            {
                // 切断済みクライアントへの書き込み失敗は無視する.
            }
        }

        private static string FormatLine(LogEntry entry) =>
            (entry.Message ?? string.Empty).Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _service.OnLogAdded -= HandleLogAdded;
            _service.OnLogRemoved -= HandleLogRemoved;
            _cts.Cancel();

            try
            {
                _listener.Stop();
            }
            catch (SocketException)
            {
            }

            lock (_clientsLock)
            {
                // 認証待ちの接続も閉じないと、読み取り待ちが解けずリークするため両方を対象にする.
                CloseAll(_clients);
                CloseAll(_pendingClients);
            }

            _cts.Dispose();
        }

        private static void CloseAll(List<TcpClient> clients)
        {
            foreach (var client in clients)
            {
                try
                {
                    client.Close();
                }
                catch (Exception)
                {
                }
            }

            clients.Clear();
        }
    }
}

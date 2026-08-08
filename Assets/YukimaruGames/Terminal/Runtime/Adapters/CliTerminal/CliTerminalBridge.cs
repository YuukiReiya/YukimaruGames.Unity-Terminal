using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Models;

namespace YukimaruGames.Terminal.Adapters.CliTerminal
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
    public sealed class CliTerminalBridge : IDisposable
    {
        /// <summary>
        /// プロンプト行(キャレット代わり)の目印文字列.
        /// </summary>
        /// <remarks>
        /// 通常のログ行(<see cref="HandleLogAdded"/>)は末尾に改行を付けて送るのに対し、
        /// プロンプトは「同じ行の続きにユーザーの入力が来る」ことを期待するため改行を付けない。
        /// ただし中継スクリプト側の受信ループは改行区切りで1行ずつ読む都合上、プロンプト自体は
        /// 改行付きの1行として送りつつ、この目印を先頭に付けることで中継スクリプト側に
        /// 「改行せず出力しろ」と伝える(<see cref="RelayScriptWriter"/>の受信ループ実装を参照).
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
        private const string AutocompleteCompleteResponsePrefix = "COMPLETE:";

        /// <summary>
        /// 自動補完の結果、候補が複数あった場合の応答の目印(直後に空白区切りの
        /// 候補一覧が続く。中継スクリプトはこれを新しい行として表示する).
        /// </summary>
        private const string AutocompleteCandidatesResponsePrefix = "CANDIDATES:";

        /// <summary>
        /// 自動補完の結果、候補が無かった場合の応答.
        /// </summary>
        private const string AutocompleteNoMatchResponse = "NOMATCH";

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
        private readonly List<TcpClient> _clients = new();
        private readonly object _clientsLock = new();
        private readonly SynchronizationContext _mainThreadContext;
        private bool _disposed;

        /// <summary>
        /// 割り当てられたループバックポート番号.
        /// </summary>
        public int Port { get; }

        public CliTerminalBridge(ITerminalService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

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

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

                    lock (_clientsLock)
                    {
                        _clients.Add(client);
                    }

                    SendPromptTo(client);
                    _ = ClientReadLoopAsync(client, ct);
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

        private async Task ClientReadLoopAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
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
            }
            catch (IOException)
            {
                // クライアント切断時の正常系.
            }
            catch (ObjectDisposedException)
            {
                // Dispose()時の正常系.
            }
            finally
            {
                lock (_clientsLock)
                {
                    _clients.Remove(client);
                }
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
                        0 => AutocompleteNoMatchResponse,
                        1 => AutocompleteCompleteResponsePrefix + results[0],
                        _ => AutocompleteCandidatesResponsePrefix + string.Join(" ", results),
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
                var bytes = Encoding.UTF8.GetBytes(PromptSentinel + _service.Prompt + "\n");
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

            var bytes = Encoding.UTF8.GetBytes(PromptSentinel + _service.Prompt + "\n");
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
                foreach (var client in _clients)
                {
                    try
                    {
                        client.Close();
                    }
                    catch (Exception)
                    {
                    }
                }

                _clients.Clear();
            }

            _cts.Dispose();
        }
    }
}

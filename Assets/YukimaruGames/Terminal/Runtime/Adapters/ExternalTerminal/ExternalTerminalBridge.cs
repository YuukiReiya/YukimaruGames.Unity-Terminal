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

namespace YukimaruGames.Terminal.Adapters.ExternalTerminal
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
    public sealed class ExternalTerminalBridge : IDisposable
    {
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

        public ExternalTerminalBridge(ITerminalService service)
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
        private Task ExecuteOnMainThreadAsync(string line, CancellationToken ct)
        {
            if (_mainThreadContext == null)
            {
                // メインスレッドのコンテキストを取得できなかった場合のフォールバック
                // (本来は起こらない想定だが、呼び出し元を巻き込まないようその場で処理する).
                return ExecuteAndLogAsync(line, ct);
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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

            return tcs.Task;
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
        }

        /// <summary>
        /// ANSIエスケープシーケンスによる画面クリア(カーソルを左上へ戻す).
        /// </summary>
        /// <remarks>
        /// 中継スクリプト(bash/PowerShell)側の受信ループは改行区切りで1行ずつ読むため、
        /// 末尾の"\n"が無いとバッファに滞留し、次の(改行付きの)出力が来るまで反映されない
        /// (=クリアが1テンポ遅れて見える不具合の原因だった).
        /// </remarks>
        private static readonly byte[] AnsiClearScreen = Encoding.UTF8.GetBytes("\x1b[2J\x1b[H\n");

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

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// Terminal.app上でbash中継スクリプト(/dev/tcp)を実行し、外部ターミナルウィンドウを起動するランチャー.
    /// </summary>
    /// <remarks>
    /// 引数のクォート崩れを避けるため、osascriptへは直接コマンド文字列を渡さず、
    /// 一時ファイルへ書き出したAppleScriptを実行する形を取っている.
    /// </remarks>
    public sealed class MacCommandLineLauncher : ICommandLineLauncher
    {
        /// <summary>
        /// タブを閉じるosascriptの完了待ち上限。closer側は確認シート出現をポーリングして
        /// 待つ場合があり最大で数秒かかりうるため、それより余裕を持った値にする
        /// (この待ち上限を超えてもosascript自体はバックグラウンドで動き続けて処理を完了させる。
        /// ここで打ち切られるのはC#側の待機だけ).
        /// </summary>
        private const int CloseTimeoutMilliseconds = 6000;

        /// <summary>
        /// 起動用osascriptの完了待ち上限。Terminal.appのactivate・custom title設定まで含めて
        /// 完了させることを優先し、通常想定される起動時間より十分長く取る(TCC権限ダイアログ等で
        /// 万一応答が返らない場合の無限ハング防止のための上限であり、通常経路では超えない想定).
        /// </summary>
        private const int LaunchTimeoutMilliseconds = 15000;

        private string _launchedSessionMarker;
        private int? _launchedWindowId;

        public bool IsSupported => true;

        /// <inheritdoc/>
        public string BuildConnectionCommand(int port, string token)
        {
            string relayPath;

            try
            {
                (relayPath, _) = PrepareRelay(token);
                CommandLineRelayScriptWriter.WritePortFile(port);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception)
            {
                // 一時ディレクトリへ書けない・chmodを起動できない等。接続手段を用意できないだけで
                // 待ち受け自体は続行できるため、契約どおりnullを返して呼び出し側の判断に委ねる.
                return null;
            }

            // 中継スクリプトは配列・local・/dev/tcp等のbash拡張に依存するため、shではなくbashで起動する
            // (/bin/shの実体はOS・設定によって変わりうる)。
            // ポートとトークンのパスはスクリプトが自分の隣から読むため、引数は付けない
            // (長いパスを2つ貼り付けさせるとコピー事故が起きやすい).
            return $"bash \"{relayPath}\"";
        }

        /// <summary>
        /// 中継スクリプトとトークンファイルを書き出し、それぞれのパスを返す.
        /// </summary>
        /// <remarks>
        /// トークンそのものではなく、トークンを書いた一時ファイルのパスだけを引数に渡す
        /// (引数は`ps`等で同一マシンの他プロセスから丸見えになり、認証の意味が薄れるため)。
        /// <para>
        /// macOSの<c>TMPDIR</c>はユーザー毎のディレクトリ(0700)だが、未設定で/tmpへ
        /// フォールバックした場合は共有ディレクトリになる。<b>トークンを書き出す前に</b>
        /// セッションディレクトリを所有者専用にし、続けてファイル自体も所有者のみ読み書き可能にする。
        /// 書き出してからchmodすると、その間だけ他ユーザーが読める状態になるため、順序に意味がある
        /// (Unityの実行環境にはUnixFileModeを指定して作成するAPIが無く、作成と同時の権限指定はできない).
        /// </para>
        /// </remarks>
        private static (string RelayPath, string TokenPath) PrepareRelay(string token)
        {
            var relayPath = CommandLineRelayScriptWriter.WriteMacRelayScript();
            MakeExecutable(relayPath);
            RestrictDirectoryToOwner(Path.GetDirectoryName(relayPath));

            var tokenPath = CommandLineRelayScriptWriter.WriteTokenFile(token);
            RestrictToOwner(tokenPath);

            return (relayPath, tokenPath);
        }

        public Process Launch(int port, string token)
        {
            var (relayPath, tokenPath) = PrepareRelay(token);
            CommandLineRelayScriptWriter.WritePortFile(port);

            var launcherPath = CommandLineRelayScriptWriter.WriteMacLauncherScript();
            var sessionMarker = CommandLineRelayScriptWriter.CreateSessionMarker();

            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"\"{launcherPath}\" \"{relayPath}\" {port} \"{tokenPath}\" \"{sessionMarker}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            };

            var process = Process.Start(startInfo);

            // osascript(launcherスクリプト)はTerminal.appのdo script・custom title設定・
            // activateまで完了してから、新規作成したウィンドウのIDを標準出力へ返して終了する。
            // ここで完了を待たずに戻ると、直後にCloseLaunchedTerminalが呼ばれた場合
            // (起動直後に接続が切れた等)、まだウィンドウIDを受け取れておらず見つけられず、
            // ウィンドウが残ってしまう。これを確実に避けるため完了を待つ
            // (Terminal.appの起動が遅い環境ではその分Openの呼び出し元をブロックするが、
            // 「閉じ忘れうる」より「起動が少し遅い」方を選ぶ).
            var windowIdText = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit(LaunchTimeoutMilliseconds);

            _launchedSessionMarker = sessionMarker;
            _launchedWindowId = int.TryParse(windowIdText, out var windowId) && windowId != 0
                ? windowId
                : null;

            return process;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="Launch"/>が起動するosascriptプロセス自体はTerminal.appへ<c>do script</c>を
        /// 依頼した直後に終了するため(Terminal.appのウィンドウそのものではない)、Killしても
        /// ウィンドウには影響しない。代わりに<see cref="Launch"/>が受け取ったウィンドウIDを
        /// 頼りに、別のosascriptで対象ウィンドウを直接指定して閉じる。
        /// Terminal.appの仕様上タブ単体は閉じられないため、そのタブがウィンドウ内で唯一の
        /// タブの場合のみウィンドウごと閉じる(詳細は<see cref="CommandLineRelayScriptWriter"/>の
        /// MacCloserScriptTemplateを参照)。
        /// </remarks>
        public void CloseLaunchedTerminal()
        {
            var sessionMarker = _launchedSessionMarker;
            var windowId = _launchedWindowId;
            _launchedSessionMarker = null;
            _launchedWindowId = null;

            if (string.IsNullOrEmpty(sessionMarker) || windowId is null)
            {
                return;
            }

            string closerPath;

            try
            {
                closerPath = CommandLineRelayScriptWriter.WriteMacCloserScript();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // セッションディレクトリが既に無い等。ウィンドウは残ってしまうが、
                // 中継そのものは正常に終わっているので握りつぶす.
                return;
            }

            try
            {
                using var closer = Process.Start(new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"\"{closerPath}\" {windowId.Value} \"{sessionMarker}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                closer?.WaitForExit(CloseTimeoutMilliseconds);
            }
            catch (Exception e) when (e is Win32Exception or InvalidOperationException)
            {
                // 後始末なので、失敗してもセッション終了処理自体は継続する.
            }
        }

        private static void MakeExecutable(string path) => Chmod("+x", path);

        private static void RestrictToOwner(string path) => Chmod("600", path);

        /// <summary>ディレクトリを所有者のみ読み書き・進入可能にする.</summary>
        private static void RestrictDirectoryToOwner(string path) => Chmod("700", path);

        private static void Chmod(string mode, string path)
        {
            using var chmod = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"{mode} \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            chmod?.WaitForExit();
        }
    }
}
#endif

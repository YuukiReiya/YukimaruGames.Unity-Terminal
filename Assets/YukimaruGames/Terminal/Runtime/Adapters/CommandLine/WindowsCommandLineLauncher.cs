#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// cmd.exe上でPowerShell中継スクリプトを実行し、外部ターミナルウィンドウを起動するランチャー.
    /// </summary>
    public sealed class WindowsCommandLineLauncher : ICommandLineLauncher
    {
        private Process _launchedProcess;

        public bool IsSupported => true;

        /// <inheritdoc/>
        public string BuildConnectionCommand(int port, string token)
        {
            string scriptPath;

            try
            {
                (scriptPath, _) = PrepareRelay(token);
                CommandLineRelayScriptWriter.WritePortFile(port);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // 一時ディレクトリへ書けない場合。接続手段を用意できないだけで待ち受け自体は
                // 続行できるため、契約どおりnullを返して呼び出し側の判断に委ねる.
                return null;
            }

            // ポートとトークンのパスはスクリプトが自分の隣から読むため、引数は付けない
            // (長いパスを2つ貼り付けさせるとコピー事故が起きやすい).
            return $"powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
        }

        /// <summary>
        /// 中継スクリプトとトークンファイルを書き出し、それぞれのパスを返す.
        /// </summary>
        /// <remarks>
        /// トークンそのものではなく、トークンを書いた一時ファイルのパスだけを引数に渡す
        /// (引数はタスクマネージャー/WMI等から他プロセスに見えてしまうため)。
        /// Windowsの一時ディレクトリはユーザー毎に分離されているため、追加のACL設定は行わない.
        /// </remarks>
        private static (string ScriptPath, string TokenPath) PrepareRelay(string token)
        {
            var scriptPath = CommandLineRelayScriptWriter.WriteWindowsRelayScript();
            var tokenPath = CommandLineRelayScriptWriter.WriteTokenFile(token);

            return (scriptPath, tokenPath);
        }

        public Process Launch(int port, string token)
        {
            var (scriptPath, tokenPath) = PrepareRelay(token);
            CommandLineRelayScriptWriter.WritePortFile(port);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Port {port} -TokenPath \"{tokenPath}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            _launchedProcess = Process.Start(startInfo);
            return _launchedProcess;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="Launch"/>が返す<see cref="Process"/>はcmd.exeウィンドウそのものを指すため
        /// (<c>UseShellExecute = true</c>で起動している)、これをKillすればウィンドウごと閉じる。
        /// macOSと異なりウィンドウの識別に別途目印は要らない.
        /// </remarks>
        public void CloseLaunchedTerminal()
        {
            try
            {
                if (_launchedProcess is { HasExited: false })
                {
                    _launchedProcess.Kill();
                }
            }
            catch (Exception e) when (e is InvalidOperationException or Win32Exception)
            {
                // 利用者が既に手動でウィンドウを閉じていた場合等。後始末なので握りつぶす.
            }
            finally
            {
                _launchedProcess?.Dispose();
                _launchedProcess = null;
            }
        }
    }
}
#endif

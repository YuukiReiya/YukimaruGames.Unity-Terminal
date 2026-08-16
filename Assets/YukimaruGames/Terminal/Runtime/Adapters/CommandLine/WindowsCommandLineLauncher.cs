#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Diagnostics;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// cmd.exe上でPowerShell中継スクリプトを実行し、外部ターミナルウィンドウを起動するランチャー.
    /// </summary>
    public sealed class WindowsCommandLineLauncher : ICommandLineLauncher
    {
        public bool IsSupported => true;

        /// <inheritdoc/>
        public string BuildConnectionCommand(int port, string token)
        {
            var (scriptPath, _) = PrepareRelay(token);
            CommandLineRelayScriptWriter.WritePortFile(port);

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

            return Process.Start(startInfo);
        }
    }
}
#endif

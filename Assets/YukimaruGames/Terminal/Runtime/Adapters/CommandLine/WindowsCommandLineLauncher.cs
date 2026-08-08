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

        public Process Launch(int port, string token)
        {
            var scriptPath = CommandLineRelayScriptWriter.WriteWindowsRelayScript();

            // トークンそのものではなく、トークンを書いた一時ファイルのパスだけを引数に渡す
            // (引数はタスクマネージャー/WMI等から他プロセスに見えてしまうため).
            // Windowsの一時ディレクトリはユーザー毎に分離されているため、追加のACL設定は行わない.
            var tokenPath = CommandLineRelayScriptWriter.WriteTokenFile(token);

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

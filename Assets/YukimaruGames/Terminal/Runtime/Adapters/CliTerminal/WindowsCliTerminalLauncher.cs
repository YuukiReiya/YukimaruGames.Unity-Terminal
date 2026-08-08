#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Diagnostics;

namespace YukimaruGames.Terminal.Adapters.CliTerminal
{
    /// <summary>
    /// cmd.exe上でPowerShell中継スクリプトを実行し、外部ターミナルウィンドウを起動するランチャー.
    /// </summary>
    public sealed class WindowsCliTerminalLauncher : ICliTerminalLauncher
    {
        public bool IsSupported => true;

        public Process Launch(int port)
        {
            var scriptPath = RelayScriptWriter.WriteWindowsRelayScript();

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/K powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Port {port}",
                UseShellExecute = true,
                CreateNoWindow = false,
            };

            return Process.Start(startInfo);
        }
    }
}
#endif

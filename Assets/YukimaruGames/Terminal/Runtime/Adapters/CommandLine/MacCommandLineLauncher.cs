#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System.Diagnostics;

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
        public bool IsSupported => true;

        public Process Launch(int port)
        {
            var relayPath = CommandLineRelayScriptWriter.WriteMacRelayScript();
            MakeExecutable(relayPath);

            var launcherPath = CommandLineRelayScriptWriter.WriteMacLauncherScript();

            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"\"{launcherPath}\" \"{relayPath}\" {port}",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            return Process.Start(startInfo);
        }

        private static void MakeExecutable(string path)
        {
            using var chmod = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            chmod?.WaitForExit();
        }
    }
}
#endif

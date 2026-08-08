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

        public Process Launch(int port, string token)
        {
            var relayPath = CommandLineRelayScriptWriter.WriteMacRelayScript();
            MakeExecutable(relayPath);

            // トークンそのものではなく、トークンを書いた一時ファイルのパスだけを引数に渡す
            // (引数は`ps`等で同一マシンの他プロセスから丸見えになり、認証の意味が薄れるため)。
            // TMPDIR未設定で/tmpへフォールバックした場合に備え、ファイル自体も所有者のみ
            // 読み書き可能にしておく.
            var tokenPath = CommandLineRelayScriptWriter.WriteTokenFile(token);
            RestrictToOwner(tokenPath);

            var launcherPath = CommandLineRelayScriptWriter.WriteMacLauncherScript();

            var startInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"\"{launcherPath}\" \"{relayPath}\" {port} \"{tokenPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            return Process.Start(startInfo);
        }

        private static void MakeExecutable(string path) => Chmod("+x", path);

        private static void RestrictToOwner(string path) => Chmod("600", path);

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

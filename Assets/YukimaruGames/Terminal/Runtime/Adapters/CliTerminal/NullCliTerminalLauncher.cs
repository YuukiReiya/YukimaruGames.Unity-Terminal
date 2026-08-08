using System.Diagnostics;

namespace YukimaruGames.Terminal.Adapters.CliTerminal
{
    /// <summary>
    /// 未対応プラットフォーム向けのNull Objectパターン実装.
    /// </summary>
    public sealed class NullCliTerminalLauncher : ICliTerminalLauncher
    {
        public bool IsSupported => false;

        public Process Launch(int port) => null;
    }
}

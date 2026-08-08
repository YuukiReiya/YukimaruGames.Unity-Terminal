using System.Diagnostics;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// 未対応プラットフォーム向けのNull Objectパターン実装.
    /// </summary>
    public sealed class NullCommandLineLauncher : ICommandLineLauncher
    {
        public bool IsSupported => false;

        public Process Launch(int port) => null;
    }
}

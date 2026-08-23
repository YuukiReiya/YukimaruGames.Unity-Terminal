using YukimaruGames.Terminal.Domain.Contracts.Attributes;

namespace YukimaruGames.Terminal.Tests.Fixtures.CommandDiscovery
{
    /// <summary>
    /// <c>CommandDiscoverer</c>の自動探索テスト用フィクスチャ.
    /// </summary>
    /// <remarks>
    /// Assembly-CSharpでもテストアセンブリ(nunit参照を持つもの)でもない独立asmdefとして、
    /// 実際の独自asmdef配下でのコマンド発見(#176)を検証するために存在する。
    /// Editor専用アセンブリのため、パッケージ利用側のPlayerビルドには含まれない.
    /// </remarks>
    public static class CommandDiscoveryFixtureCommands
    {
        /// <summary>
        /// 独自asmdef配下の静的メソッドが発見されることを検証するためのサンプルコマンド.
        /// </summary>
        [TerminalCommand("discoverertest.sample", maxArgCount: 1, minArgCount: 0, help: "sample")]
        public static void SampleCommand(string arg)
        {
        }

        /// <summary>
        /// コマンド名が空のメソッドが検出結果から除外されることを検証するためのサンプルコマンド.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        [TerminalCommand("")]
        public static void EmptyCommandName()
        {
        }
    }

    /// <summary>
    /// インスタンスメソッドが除外されることを検証するためのフィクスチャ.
    /// </summary>
    public sealed class CommandDiscoveryInstanceFixture
    {
        /// <summary>
        /// インスタンスメソッドが検出結果から除外されることを検証するためのサンプルコマンド.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        [TerminalCommand("discoverertest.instance")]
        public void InstanceCommand()
        {
        }
    }
}

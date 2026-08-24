using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Infrastructure.Discoverer;

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
        /// <remarks>
        /// このアセンブリはEditor上で常時ロードされる(#178フォローアップで判明)ため、
        /// <see cref="SuppressCommandDiscoveryWarningAttribute"/>で発見不可の警告ログだけを
        /// 抑制している。除外判定(<c>IsDiscoverable</c>)自体はこれまで通り実行される.
        /// </remarks>
        // ReSharper disable once UnusedMember.Global
        [TerminalCommand("")]
        [SuppressCommandDiscoveryWarning]
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
        /// <remarks>
        /// <c>CommandDiscoverer</c>の走査は<c>BindingFlags.Static</c>のみを対象とするため、
        /// インスタンスメソッドである本メソッドはそもそも列挙されず警告も発生しない。
        /// 将来の走査対象変更に備えた保険として<see cref="SuppressCommandDiscoveryWarningAttribute"/>
        /// を付与している.
        /// </remarks>
        // ReSharper disable once UnusedMember.Global
        [TerminalCommand("discoverertest.instance")]
        [SuppressCommandDiscoveryWarning]
        public void InstanceCommand()
        {
        }
    }
}

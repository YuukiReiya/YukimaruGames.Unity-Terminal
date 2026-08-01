using YukimaruGames.Terminal.Presentation.Contracts;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// 何もしない Null Object パターン実装.
    /// <see cref="TerminalNullInstaller"/>など、Viewを構築しない構成でのフォールバック先.
    /// </summary>
    public sealed class NullTerminalView : ITerminalView
    {
        void ITerminalView.SetVisible(bool visible)
        {
            // 何もしない.
        }
    }
}

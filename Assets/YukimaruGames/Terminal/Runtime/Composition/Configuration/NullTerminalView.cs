using YukimaruGames.Terminal.Presentation.Contracts;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 何もしない Null Object パターン実装.
    /// <see cref="NullInstaller"/>など、Viewを構築しない構成でのフォールバック先.
    /// </summary>
    public sealed class NullTerminalView : ITerminalView
    {
        void ITerminalView.SetVisible(bool visible)
        {
            // 何もしない.
        }
    }
}

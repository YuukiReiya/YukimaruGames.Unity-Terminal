using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Models.Launcher
{
    public readonly struct LauncherRenderData
    {
        public bool IsVisible { get; }
        public bool IsReverse { get; }
        public TerminalRect WindowRect { get; }
        public WindowAnchor Anchor { get; }

        public LauncherRenderData(bool isVisible,bool isReverse,TerminalRect windowRect,WindowAnchor anchor)
        {
            IsVisible = isVisible;
            IsReverse = isReverse;
            WindowRect = windowRect;
            Anchor = anchor;
        }
    }
}

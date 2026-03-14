using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Launcher
{
    public readonly struct LauncherRenderData
    {
        public bool IsVisible { get; }
        public bool IsReverse { get; }
        public Rect WindowRect { get; }
        public TerminalAnchor Anchor { get; }

        public LauncherRenderData(bool isVisible,bool isReverse,Rect windowRect,TerminalAnchor anchor)
        {
            IsVisible = isVisible;
            IsReverse = isReverse;
            WindowRect = windowRect;
            Anchor = anchor;
        }
    }
}

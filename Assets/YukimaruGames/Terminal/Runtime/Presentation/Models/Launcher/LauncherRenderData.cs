using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Models.Launcher
{
    public readonly struct LauncherRenderData
    {
        public bool IsVisible { get; }
        public bool IsReverse { get; }
        public Rect WindowRect { get; }
        public WindowAnchor Anchor { get; }

        public LauncherRenderData(bool isVisible,bool isReverse,Rect windowRect,WindowAnchor anchor)
        {
            IsVisible = isVisible;
            IsReverse = isReverse;
            WindowRect = windowRect;
            Anchor = anchor;
        }
    }
}

using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalButtonRenderData
    {
        public bool IsVisible { get; }
        public bool IsReverse { get; }
        public Rect WindowRect { get; }
        public TerminalAnchor Anchor { get; }

        public TerminalButtonRenderData(bool isVisible,bool isReverse,Rect windowRect,TerminalAnchor anchor)
        {
            IsVisible = isVisible;
            IsReverse = isReverse;
            WindowRect = windowRect;
            Anchor = anchor;
        }
    }
}

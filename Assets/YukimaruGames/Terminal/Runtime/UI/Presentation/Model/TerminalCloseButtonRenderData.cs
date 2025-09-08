using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalCloseButtonRenderData
    {
        public bool IsVisible { get; }
        public bool IsReverse { get; }
        public Rect WindowRect { get; }
        public TerminalAnchor Anchor { get; }

        public TerminalCloseButtonRenderData(bool isVisible, bool isReverse, Rect windowRect, TerminalAnchor anchor)
        {
            IsVisible = isVisible;
            IsReverse = isReverse;
            WindowRect = windowRect;
            Anchor = anchor;
        }
    }
}

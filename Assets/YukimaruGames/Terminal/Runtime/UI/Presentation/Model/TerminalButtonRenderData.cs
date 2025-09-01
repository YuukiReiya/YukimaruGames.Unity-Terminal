using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalButtonRenderData
    {
        public bool IsVisible { get; }
        public Rect OpenButtonsRect { get; }
        public TerminalAnchor Anchor { get; }

        public TerminalButtonRenderData(bool isVisible, Rect openButtonsRect, TerminalAnchor anchor)
        {
            IsVisible = isVisible;
            OpenButtonsRect = openButtonsRect;
            Anchor = anchor;
        }
    }
}

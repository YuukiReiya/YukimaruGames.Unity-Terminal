using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalButtonVisibleProvider
    {
        bool IsVisible { get; }
        bool IsReverse { get; }
        
        event Action<bool> OnVisibleChanged;
        event Action<bool> OnReverseChanged;
    }
}

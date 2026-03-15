using System;

namespace YukimaruGames.Terminal.UI.Launcher
{
    public interface ILauncherVisibleProvider
    {
        bool IsVisible { get; }
        bool IsReverse { get; }
        
        event Action<bool> OnVisibleChanged;
        event Action<bool> OnReverseChanged;
    }
}

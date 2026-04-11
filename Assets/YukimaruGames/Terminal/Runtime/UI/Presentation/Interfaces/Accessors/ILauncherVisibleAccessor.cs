using System;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface ILauncherVisibleAccessor :
        ILauncherVisibleMutator,
        ILauncherVisibleProvider
    {
        new bool IsVisible { get; set; }
        new bool IsReverse { get; set; }
    }

    public interface ILauncherVisibleMutator
    {
        bool IsVisible { set; }
        bool IsReverse { set; }
    }
    
    public interface ILauncherVisibleProvider
    {
        bool IsVisible { get; }
        bool IsReverse { get; }
        
        event Action<bool> OnVisibleChanged;
        event Action<bool> OnReverseChanged;
    }
}

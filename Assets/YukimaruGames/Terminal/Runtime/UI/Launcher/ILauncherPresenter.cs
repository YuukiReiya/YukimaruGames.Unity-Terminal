using System;

namespace YukimaruGames.Terminal.UI.Launcher
{
    public interface ILauncherPresenter
    {
        event Action OnOpenTriggered;
        event Action OnCloseTriggered;
    }
}

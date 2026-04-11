using System;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Presenters
{
    public interface ILauncherPresenter
    {
        event Action OnOpenTriggered;
        event Action OnCloseTriggered;
    }
}

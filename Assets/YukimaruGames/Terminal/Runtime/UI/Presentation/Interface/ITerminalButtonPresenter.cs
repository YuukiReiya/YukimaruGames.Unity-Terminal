using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalButtonPresenter
    {
        event Action OnOpenTriggered;
        event Action OnCloseTriggered;
    }
}

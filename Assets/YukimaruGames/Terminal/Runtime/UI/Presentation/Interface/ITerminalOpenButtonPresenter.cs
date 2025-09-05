using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalOpenButtonPresenter
    {
        event Action OnCompactOpenTriggered;
        event Action OnFullOpenTriggered;
    }
}

using System;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Events
{
    public interface IEventListener
    {
        bool IsEnabled { get; set; }
        event Action OnOpenTriggered;
        event Action OnCloseTriggered;
        event Action OnExecuteTriggered;
        event Action OnPreviousHistoryTriggered;
        event Action OnNextHistoryTriggered;
        event Action OnAutocompleteTriggered;
        event Action OnFocusTriggered;
    }
}

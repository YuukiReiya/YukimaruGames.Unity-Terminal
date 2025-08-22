using System;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalEventListener
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

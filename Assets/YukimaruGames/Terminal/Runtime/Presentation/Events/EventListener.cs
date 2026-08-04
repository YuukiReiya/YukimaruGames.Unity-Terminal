using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Models.Event;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Presentation.Events
{
    public sealed class EventListener : IEventListener, IUpdatable
    {
        private IKeyboardInputHandler _inputHandler;
        private bool _isEnable = true;

        public event Action OnOpenTriggered;
        public event Action OnCloseTriggered;
        public event Action OnExecuteTriggered;
        public event Action OnCancelTriggered;
        public event Action OnPreviousHistoryTriggered;
        public event Action OnNextHistoryTriggered;
        public event Action OnAutocompleteTriggered;
        public event Action OnFocusTriggered;

        public bool IsEnabled
        {
            get => _isEnable && _inputHandler != null;
            set => _isEnable = value;
        }

        public EventListener(IKeyboardInputHandler handler)
        {
            _inputHandler = handler;
        }

        public void SetInputHandler(IKeyboardInputHandler inputHandler)
        {
            _inputHandler = inputHandler;
        }

        private void Update()
        {
            if (!IsEnabled) return;

            if (_inputHandler.WasTriggered(TerminalAction.Open)) OnOpenTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.Close)) OnCloseTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.Execute)) OnExecuteTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.Cancel)) OnCancelTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.PreviousHistory)) OnPreviousHistoryTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.NextHistory)) OnNextHistoryTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.Autocomplete)) OnAutocompleteTriggered?.Invoke();
            if (_inputHandler.WasTriggered(TerminalAction.Focus)) OnFocusTriggered?.Invoke();
        }

        public void Update(float _) => Update();
    }
}

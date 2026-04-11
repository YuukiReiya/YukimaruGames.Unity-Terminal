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

            if (_inputHandler.WasReleasedThisFrame(Trigger.Open)) OnOpenTriggered?.Invoke();
            if (_inputHandler.WasReleasedThisFrame(Trigger.Close)) OnCloseTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(Trigger.Execute)) OnExecuteTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(Trigger.PreviousHistory)) OnPreviousHistoryTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(Trigger.NextHistory)) OnNextHistoryTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(Trigger.Autocomplete)) OnAutocompleteTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(Trigger.Focus)) OnFocusTriggered?.Invoke();
        }

        public void Update(float _) => Update();
    }
}

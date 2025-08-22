using System;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.View.Input
{
    public sealed class TerminalEventListener : ITerminalEventListener, IUpdatable
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

        public TerminalEventListener(IKeyboardInputHandler handler)
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

            if (_inputHandler.WasReleasedThisFrame(TerminalAction.Open)) OnOpenTriggered?.Invoke();
            if (_inputHandler.WasReleasedThisFrame(TerminalAction.Close)) OnCloseTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(TerminalAction.Execute)) OnExecuteTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(TerminalAction.PreviousHistory)) OnPreviousHistoryTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(TerminalAction.NextHistory)) OnNextHistoryTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(TerminalAction.Autocomplete)) OnAutocompleteTriggered?.Invoke();
            if (_inputHandler.WasPressedThisFrame(TerminalAction.Focus)) OnFocusTriggered?.Invoke();
        }

        public void Update(float _) => Update();
    }
}

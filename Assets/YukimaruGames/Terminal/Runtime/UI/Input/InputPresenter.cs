using System;

namespace YukimaruGames.Terminal.UI.Input
{
    public sealed class InputPresenter : IInputPresenter, IDisposable
    {
        private readonly IInputRenderer _renderer;

        public InputPresenter(IInputRenderer renderer,string bootupCommand)
        {
            _renderer = renderer;
            _renderer.OnInputTextChanged += HandleTextChanged;
            _renderer.OnFocusControlChanged += HandleFocusChanged;
            _renderer.OnMoveCursorToEndTriggerChanged += HandleMoveCursorToEndTriggerChanged;
            _renderer.OnImeComposingStateChanged += HandleImeComposingStateChanged;
            
            SetInputField(bootupCommand);
        }

        public string InputText { get; private set; }
        public bool IsImeComposing { get; private set; }

        public bool IsEditable { get; set; } = true;
        private bool _isMoveCursorToEnd;

        public void SetInputField(string inputText)
        {
            InputText = inputText;
        }

        public void SetFocus(bool focus)
        {
            Focus = focus ? Focus.Apply : Focus.Release;
        }

        public void SetMoveCursorToEnd() => _isMoveCursorToEnd = true;

        InputRenderData IInputRenderDataProvider.GetRenderData()
        {
            return new InputRenderData(InputText, Focus, _isMoveCursorToEnd);
        }

        private Focus Focus { get; set; }

        private void HandleTextChanged(string input)
        {
            InputText = !IsEditable ? string.Empty : input;
        }

        private void HandleFocusChanged(Focus focus)
        {
            Focus = focus;
        }

        private void HandleMoveCursorToEndTriggerChanged(bool moveCursorToEnd)
        {
            _isMoveCursorToEnd = moveCursorToEnd;
        }

        private void HandleImeComposingStateChanged(bool isImeComposing)
        {
            IsImeComposing = isImeComposing;
        }
        
        public void Dispose()
        {
            if (_renderer != null)
            {
                _renderer.OnInputTextChanged -= HandleTextChanged;
                _renderer.OnFocusControlChanged -= HandleFocusChanged;
                _renderer.OnMoveCursorToEndTriggerChanged -= HandleMoveCursorToEndTriggerChanged;
                _renderer.OnImeComposingStateChanged -= HandleImeComposingStateChanged;
            }
        }
    }
}

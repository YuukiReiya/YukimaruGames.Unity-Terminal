using System;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalInputPresenter : ITerminalInputPresenter, IDisposable
    {
        private readonly ITerminalInputRenderer _renderer;

        public TerminalInputPresenter(ITerminalInputRenderer renderer,string bootupCommand)
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
            FocusControl = focus ? FocusControl.Focus : FocusControl.UnFocus;
        }

        public void SetMoveCursorToEnd() => _isMoveCursorToEnd = true;

        TerminalInputRenderData ITerminalInputRenderDataProvider.GetRenderData()
        {
            return new TerminalInputRenderData(InputText, FocusControl, _isMoveCursorToEnd);
        }

        private FocusControl FocusControl { get; set; }

        private void HandleTextChanged(string input)
        {
            InputText = !IsEditable ? string.Empty : input;
        }

        private void HandleFocusChanged(FocusControl focus)
        {
            FocusControl = focus;
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

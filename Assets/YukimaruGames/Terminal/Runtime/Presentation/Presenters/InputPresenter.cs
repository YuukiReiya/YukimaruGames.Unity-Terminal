using System;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Presenters
{
    /// <summary>
    /// 入力欄の状態を管理し、<see cref="IInputProvider"/>から通知される入力イベントを反映するPresenter.
    /// </summary>
    public sealed class InputPresenter : IInputPresenter, IDisposable
    {
        private readonly IInputProvider _inputProvider;

        public InputPresenter(IInputProvider inputProvider, string bootupCommand)
        {
            _inputProvider = inputProvider;
            _inputProvider.OnInputTextChanged += HandleTextChanged;
            _inputProvider.OnFocusControlChanged += HandleFocusChanged;
            _inputProvider.OnMoveCursorToEndTriggerChanged += HandleMoveCursorToEndTriggerChanged;
            _inputProvider.OnImeComposingStateChanged += HandleImeComposingStateChanged;

            SetInputField(bootupCommand);
        }

        public string InputText { get; private set; }
        public bool IsImeComposing { get; private set; }

        public bool IsEditable { get; set; } = true;

        /// <inheritdoc/>
        /// <remarks>
        /// <see cref="Focus"/>(命令)とは別に保持する。<see cref="WindowFocus"/>の扱いは
        /// バックエンドによって異なり、IMGUI版は「一度きりの命令」として適用後に自ら
        /// <see cref="WindowFocus.None"/>へ戻すため、命令のチャンネルから状態を読むと
        /// 実際にはフォーカスがあるのに<c>false</c>になる(実機で確認)。
        /// uGUI版・UIToolkit版は実際のフォーカス変化を<see cref="WindowFocus.Apply"/> /
        /// <see cref="WindowFocus.Release"/>で通知するため、そちらだけを状態の更新に使う.
        /// </remarks>
        public bool IsFocused { get; private set; }

        private bool _isMoveCursorToEnd;

        public void SetInputField(string inputText)
        {
            InputText = inputText;
        }

        public void SetFocus(bool focus)
        {
            IsFocused = focus;
            Focus = focus ? WindowFocus.Apply : WindowFocus.Release;
        }

        public void SetMoveCursorToEnd() => _isMoveCursorToEnd = true;

        InputRenderData IInputRenderDataProvider.RenderData { get=> new InputRenderData(InputText, Focus, _isMoveCursorToEnd); }

        private WindowFocus Focus { get; set; }

        private void HandleTextChanged(string input)
        {
            InputText = !IsEditable ? string.Empty : input;
        }

        private void HandleFocusChanged(WindowFocus focus)
        {
            Focus = focus;

            // Noneは「命令を消化した」の意味であり、フォーカスを失ったわけではないため状態は変えない.
            switch (focus)
            {
                case WindowFocus.Apply:
                    IsFocused = true;
                    break;
                case WindowFocus.Release:
                    IsFocused = false;
                    break;
            }
        }

        private void HandleMoveCursorToEndTriggerChanged(bool moveCursorToEnd)
        {
            _isMoveCursorToEnd = moveCursorToEnd;
        }

        private void HandleImeComposingStateChanged(bool isImeComposing)
        {
            IsImeComposing = isImeComposing;
        }
        
        void IDisposable.Dispose()
        {
            if (_inputProvider != null)
            {
                _inputProvider.OnInputTextChanged -= HandleTextChanged;
                _inputProvider.OnFocusControlChanged -= HandleFocusChanged;
                _inputProvider.OnMoveCursorToEndTriggerChanged -= HandleMoveCursorToEndTriggerChanged;
                _inputProvider.OnImeComposingStateChanged -= HandleImeComposingStateChanged;
            }
        }
    }
}

#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.UIToolkit.Renderers
{
    /// <summary>
    /// UIToolkit(<see cref="TextField"/>)による入力欄の描画と、入力イベントの通知を行う.
    /// </summary>
    public sealed class InputRenderer : IInputRenderer, IInputProvider, IDisposable
    {
        private readonly TextField _textField;
        private readonly IScrollMutator _scrollMutator;
        private readonly CursorView _cursorView;

        private bool _isCurrentlyFocused;
        private bool _isImeComposing;

        public event Action<string> OnInputTextChanged;
        public event Action<WindowFocus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        public InputRenderer(TextField textField, IScrollMutator scrollMutator, CursorView cursorView)
        {
            _textField = textField;
            _scrollMutator = scrollMutator;
            _cursorView = cursorView;

            if (_textField == null) return;

            _textField.RegisterValueChangedCallback(OnValueChanged);
            _textField.RegisterCallback<FocusInEvent>(OnFocusIn);
            _textField.RegisterCallback<FocusOutEvent>(OnFocusOut);

            // Escape/TabはTextFieldのネイティブなデフォルト動作(Escape=編集内容を直前の値へ
            // ロールバック、Tab=次のフォーカス可能要素へ移動)と、ターミナル側の独自バインド
            // (既定でEscape=ウィンドウを閉じる、Tab=オートコンプリート)が衝突する。
            // TrickleDown(capture)フェーズで先取りしてStopPropagationし、ネイティブの
            // デフォルト動作(KeyboardTextEditorEventHandler等)に到達させない。
            // ターミナル側のバインド自体はUpdate()駆動の別経路(IKeyboardInputHandler)で
            // 判定されるため、ここで止めても機能しなくなることはない。
            _textField.RegisterCallback<KeyDownEvent>(OnKeyDownCapture, TrickleDown.TrickleDown);
        }

        public void Render(InputRenderData data)
        {
            if (_textField == null) return;

            if (!string.Equals(_textField.value, data.InputText, StringComparison.Ordinal))
            {
                _textField.SetValueWithoutNotify(data.InputText);
            }

            ApplyFocus(data.Focus);

            if (data.IsMoveCursorToEnd)
            {
                MoveCursorToEnd();
                OnMoveCursorToEndTriggerChanged?.Invoke(false);
            }

            PollImeComposingState();
        }

        private void ApplyFocus(WindowFocus focus)
        {
            switch (focus)
            {
                case WindowFocus.Apply:
                    if (!_isCurrentlyFocused) _textField.Focus();
                    break;
                case WindowFocus.Release:
                    if (_isCurrentlyFocused) _textField.Blur();
                    break;
            }
        }

        private void MoveCursorToEnd()
        {
            // フォーカスが無い状態でcursorIndex/selectIndexを書き換えると、ネイティブ側の
            // テキスト編集バッファ(TextEditingUtilities)と同期が取れないまま残り、次に実際の
            // キー入力(IME経由の合成含む)が来た際にUnity内部でArgumentOutOfRangeExceptionが
            // 発生することがある(実機検証で確認)。フォーカス中のみ、かつ現在のテキスト長で
            // clampした値を設定する。
            if (_textField.focusController?.focusedElement != _textField) return;

            var end = _textField.text?.Length ?? 0;
            _textField.cursorIndex = end;
            _textField.selectIndex = end;
        }

        private void PollImeComposingState()
        {
            var composing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
            if (_isImeComposing == composing) return;

            _isImeComposing = composing;
            OnImeComposingStateChanged?.Invoke(composing);
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            OnInputTextChanged?.Invoke(evt.newValue);
            _scrollMutator?.ScrollToEnd();
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            _isCurrentlyFocused = true;
            OnFocusControlChanged?.Invoke(WindowFocus.Apply);
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            _isCurrentlyFocused = false;
            OnFocusControlChanged?.Invoke(WindowFocus.Release);
        }

        private void OnKeyDownCapture(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Escape or KeyCode.Tab)
            {
                evt.StopPropagation();
            }
        }

        void IDisposable.Dispose()
        {
            if (_textField != null)
            {
                _textField.UnregisterValueChangedCallback(OnValueChanged);
                _textField.UnregisterCallback<FocusInEvent>(OnFocusIn);
                _textField.UnregisterCallback<FocusOutEvent>(OnFocusOut);
                _textField.UnregisterCallback<KeyDownEvent>(OnKeyDownCapture, TrickleDown.TrickleDown);
            }

            OnInputTextChanged = null;
            OnFocusControlChanged = null;
            OnMoveCursorToEndTriggerChanged = null;
            OnImeComposingStateChanged = null;
        }
    }
}
#endif

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.Input.InputSystem
{
    /// <summary>
    /// UnityEngine.InputSystemを用いた<see cref="IInputProvider"/>実装.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 現行の標準パイプラインでは、IME対応済みのOnGUI TextField（Adapters/GUI/Renderers/InputRenderer）が
    /// <see cref="IInputProvider"/>を実装し、既定のDI配線もそちらに接続されている。
    /// </para>
    /// <para>
    /// 本アダプタは、IMGUI以外のView実装（将来対応）向けに、InputSystemベースで
    /// 入力イベントを検出する代替の入力ソースとして提供する。IME変換状態の検知には
    /// InputSystem固有のAPIが無いため、Legacy Input Manager互換の
    /// <see cref="UnityEngine.Input.compositionString"/>を併用する
    /// （プロジェクトの Active Input Handling が「Both」の場合のみ有効）。
    /// </para>
    /// </remarks>
    public sealed class InputSystemAdapter : MonoBehaviour, IInputProvider
    {
        private string _inputText = string.Empty;
        private bool _isFocused;
        private bool _isImeComposing;

        public event Action<string> OnInputTextChanged;
        public event Action<WindowFocus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        private void OnEnable()
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput += HandleTextInput;
            }
        }

        private void OnDisable()
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= HandleTextInput;
            }
        }

        private void Update()
        {
            if (!_isFocused) return;

            DetectComposingStateChanged();
        }

        /// <summary>入力欄へのフォーカス状態を設定する.</summary>
        public void SetFocus(bool focused)
        {
            if (_isFocused == focused) return;

            _isFocused = focused;
            OnFocusControlChanged?.Invoke(focused ? WindowFocus.Apply : WindowFocus.Release);
        }

        /// <summary>保持している入力文字列を設定する（外部からの初期化・クリア用）.</summary>
        public void SetInputText(string text)
        {
            _inputText = text ?? string.Empty;
        }

        private void HandleTextInput(char c)
        {
            if (!_isFocused) return;

            switch (c)
            {
                case '\b':
                    if (_inputText.Length == 0) return;
                    _inputText = _inputText[..^1];
                    break;
                case '\n':
                case '\r':
                    return;
                default:
                    _inputText += c;
                    break;
            }

            OnInputTextChanged?.Invoke(_inputText);
            OnMoveCursorToEndTriggerChanged?.Invoke(true);
        }

        private void DetectComposingStateChanged()
        {
            var isComposing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
            if (_isImeComposing == isComposing) return;

            _isImeComposing = isComposing;
            OnImeComposingStateChanged?.Invoke(isComposing);
        }
    }
}

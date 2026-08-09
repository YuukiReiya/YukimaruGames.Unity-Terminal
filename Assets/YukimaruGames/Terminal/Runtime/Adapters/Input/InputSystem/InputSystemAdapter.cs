using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.Input.InputSystem
{
    /// <summary>
    /// UnityEngine.InputSystemを用いた<see cref="IInputProvider"/>実装.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 現行の標準パイプラインでは、IME対応済みのOnGUI TextField（Adapters/IMGUI/Renderers/InputRenderer）が
    /// <see cref="IInputProvider"/>を実装し、既定のDI配線もそちらに接続されている。
    /// </para>
    /// <para>
    /// 本アダプタは、IMGUI以外のView実装（将来対応）向けに、InputSystemベースで
    /// 入力イベントを検出する代替の入力ソースとして提供する。IME変換状態の検知には
    /// <see cref="Keyboard.onIMECompositionChange"/>（com.unity.inputsystem 1.11.2以降）を用いる。
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
                Keyboard.current.onIMECompositionChange += HandleCompositionChanged;
            }
        }

        private void OnDisable()
        {
            if (Keyboard.current != null)
            {
                Keyboard.current.onTextInput -= HandleTextInput;
                Keyboard.current.onIMECompositionChange -= HandleCompositionChanged;
            }
        }

        /// <summary>入力欄へのフォーカス状態を設定する.</summary>
        public void SetFocus(bool focused)
        {
            if (_isFocused == focused) return;

            _isFocused = focused;
            OnFocusControlChanged?.Invoke(focused ? WindowFocus.Apply : WindowFocus.Release);

            if (!focused)
            {
                SetComposingState(false);
            }
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

        /// <summary>
        /// <see cref="Keyboard.onIMECompositionChange"/>からのコールバックを処理する.
        /// </summary>
        /// <remarks>テストから直接呼び出せるようinternalにしている.</remarks>
        internal void HandleCompositionChanged(IMECompositionString composition)
        {
            if (!_isFocused) return;

            SetComposingState(composition.Count > 0);
        }

        private void SetComposingState(bool isComposing)
        {
            if (_isImeComposing == isComposing) return;

            _isImeComposing = isComposing;
            OnImeComposingStateChanged?.Invoke(isComposing);
        }
    }
}

using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.Input
{
    /// <summary>
    /// Legacy Input Manager（<see cref="UnityEngine.Input"/>）を用いた<see cref="IInputProvider"/>実装.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 現行の標準パイプラインでは、IME対応済みのOnGUI TextField
    /// （<see cref="GUI.Renderers.InputRenderer"/>）が<see cref="IInputProvider"/>を実装し、
    /// 既定のDI配線もそちらに接続されている。
    /// </para>
    /// <para>
    /// 本アダプタは、IMGUI以外のView実装（将来対応）向けに、Legacy Input Managerベースで
    /// 入力イベントを検出する代替の入力ソースとして提供する。
    /// </para>
    /// </remarks>
    public sealed class LegacyInputAdapter : MonoBehaviour, IInputProvider
    {
        private string _inputText = string.Empty;
        private bool _isFocused;
        private bool _isImeComposing;

        public event Action<string> OnInputTextChanged;
        public event Action<WindowFocus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        private void Update()
        {
            if (!_isFocused) return;

            DetectComposingStateChanged();
            DetectCharacterInput();
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

        private void DetectComposingStateChanged()
        {
            var isComposing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
            if (_isImeComposing == isComposing) return;

            _isImeComposing = isComposing;
            OnImeComposingStateChanged?.Invoke(isComposing);
        }

        private void DetectCharacterInput()
        {
            var inputString = UnityEngine.Input.inputString;
            if (string.IsNullOrEmpty(inputString)) return;

            var changed = false;
            foreach (var c in inputString)
            {
                switch (c)
                {
                    case '\b':
                        if (_inputText.Length == 0) continue;
                        _inputText = _inputText[..^1];
                        changed = true;
                        continue;
                    case '\n':
                    case '\r':
                        continue;
                    default:
                        _inputText += c;
                        changed = true;
                        continue;
                }
            }

            if (!changed) return;

            OnInputTextChanged?.Invoke(_inputText);
            OnMoveCursorToEndTriggerChanged?.Invoke(true);
        }
    }
}

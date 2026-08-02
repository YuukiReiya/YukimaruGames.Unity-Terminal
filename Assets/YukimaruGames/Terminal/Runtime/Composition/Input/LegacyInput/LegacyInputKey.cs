#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_LEGACY_INPUT_MANAGER
#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Event;
using YukimaruGames.Terminal.Presentation.Models.Input;

namespace YukimaruGames.Terminal.Composition.Input.LegacyInput
{
    [Serializable]
    public sealed class LegacyInputKey : IInputKeyMap<KeyCode>
    {
        [SerializeField] private KeyCode _openKeyCode = KeyCode.LeftBracket;
        [SerializeField] private KeyCode _openModifierKeyCode = KeyCode.None;
        [SerializeField] private KeyCode _closeKeyCode = KeyCode.Escape;
        [SerializeField] private KeyCode _closeModifierKeyCode = KeyCode.None;
        [SerializeField] private KeyCode _executeKeyCode = KeyCode.Return;
        [SerializeField] private KeyCode _executeModifierKeyCode = KeyCode.None;
        [SerializeField] private KeyCode _cancelKeyCode = KeyCode.C;
        [SerializeField] private KeyCode _cancelModifierKeyCode = KeyCode.LeftControl;
        [SerializeField] private KeyCode _prevHistoryKeyCode = KeyCode.UpArrow;
        [SerializeField] private KeyCode _prevHistoryModifierKeyCode = KeyCode.None;
        [SerializeField] private KeyCode _nextHistoryKeyCode = KeyCode.DownArrow;
        [SerializeField] private KeyCode _nextHistoryModifierKeyCode = KeyCode.None;
        [SerializeField] private KeyCode _autocompleteKeyCode = KeyCode.Tab;
        [SerializeField] private KeyCode _autocompleteModifierKeyCode = KeyCode.None;
        [SerializeField] private KeyCode _focusKeyCode = KeyCode.LeftControl;
        [SerializeField] private KeyCode _focusModifierKeyCode = KeyCode.None;

        public KeyCode GetKey(TerminalAction action) => action switch
        {
            TerminalAction.None => KeyCode.None,
            TerminalAction.Open => _openKeyCode,
            TerminalAction.Close => _closeKeyCode,
            TerminalAction.Execute => _executeKeyCode,
            TerminalAction.Cancel => _cancelKeyCode,
            TerminalAction.PreviousHistory => _prevHistoryKeyCode,
            TerminalAction.NextHistory => _nextHistoryKeyCode,
            TerminalAction.Autocomplete => _autocompleteKeyCode,
            TerminalAction.Focus => _focusKeyCode,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        /// <inheritdoc/>
        /// <remarks>アクションごとに任意の修飾キーを設定できる(既定は<see cref="KeyCode.None"/> = 修飾キー不要).</remarks>
        public KeyCode GetModifier(TerminalAction action) => action switch
        {
            TerminalAction.None => KeyCode.None,
            TerminalAction.Open => _openModifierKeyCode,
            TerminalAction.Close => _closeModifierKeyCode,
            TerminalAction.Execute => _executeModifierKeyCode,
            TerminalAction.Cancel => _cancelModifierKeyCode,
            TerminalAction.PreviousHistory => _prevHistoryModifierKeyCode,
            TerminalAction.NextHistory => _nextHistoryModifierKeyCode,
            TerminalAction.Autocomplete => _autocompleteModifierKeyCode,
            TerminalAction.Focus => _focusModifierKeyCode,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
#endif

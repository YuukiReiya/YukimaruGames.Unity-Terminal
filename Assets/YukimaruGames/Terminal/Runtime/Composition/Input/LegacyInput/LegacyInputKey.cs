#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_LEGACY_INPUT_MANAGER
#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Event;
using YukimaruGames.Terminal.Presentation.Models.Input;

namespace YukimaruGames.Terminal.Composition.Input.LegacyInput
{
    [Serializable]
    public sealed class LegacyInputKey : IInputKeyMap<KeyCode>
    {
        [SerializeField] private KeyCode _openKeyCode = KeyCode.LeftBracket;
        [SerializeField] private KeyCode[] _openModifierKeyCodes = Array.Empty<KeyCode>();
        [SerializeField] private KeyCode _closeKeyCode = KeyCode.Escape;
        [SerializeField] private KeyCode[] _closeModifierKeyCodes = Array.Empty<KeyCode>();
        [SerializeField] private KeyCode _executeKeyCode = KeyCode.Return;
        [SerializeField] private KeyCode[] _executeModifierKeyCodes = Array.Empty<KeyCode>();
        [SerializeField] private KeyCode _cancelKeyCode = KeyCode.C;
        [SerializeField] private KeyCode[] _cancelModifierKeyCodes = { KeyCode.LeftControl };
        [SerializeField] private KeyCode _prevHistoryKeyCode = KeyCode.UpArrow;
        [SerializeField] private KeyCode[] _prevHistoryModifierKeyCodes = Array.Empty<KeyCode>();
        [SerializeField] private KeyCode _nextHistoryKeyCode = KeyCode.DownArrow;
        [SerializeField] private KeyCode[] _nextHistoryModifierKeyCodes = Array.Empty<KeyCode>();
        [SerializeField] private KeyCode _autocompleteKeyCode = KeyCode.Tab;
        [SerializeField] private KeyCode[] _autocompleteModifierKeyCodes = Array.Empty<KeyCode>();
        [SerializeField] private KeyCode _focusKeyCode = KeyCode.LeftControl;
        [SerializeField] private KeyCode[] _focusModifierKeyCodes = Array.Empty<KeyCode>();

        /// <inheritdoc/>
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
        /// <remarks>アクションごとに任意個の修飾キーを設定できる(既定は空 = 修飾キー不要).</remarks>
        public IReadOnlyList<KeyCode> GetModifiers(TerminalAction action) => action switch
        {
            TerminalAction.None => Array.Empty<KeyCode>(),
            TerminalAction.Open => _openModifierKeyCodes,
            TerminalAction.Close => _closeModifierKeyCodes,
            TerminalAction.Execute => _executeModifierKeyCodes,
            TerminalAction.Cancel => _cancelModifierKeyCodes,
            TerminalAction.PreviousHistory => _prevHistoryModifierKeyCodes,
            TerminalAction.NextHistory => _nextHistoryModifierKeyCodes,
            TerminalAction.Autocomplete => _autocompleteModifierKeyCodes,
            TerminalAction.Focus => _focusModifierKeyCodes,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
#endif

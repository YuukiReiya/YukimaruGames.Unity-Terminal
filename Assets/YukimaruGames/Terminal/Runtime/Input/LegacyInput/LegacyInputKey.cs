#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_LEGACY_INPUT_MANAGER
#if ENABLE_LEGACY_INPUT_MANAGER
using System;

using UnityEngine;
using YukimaruGames.Terminal.UI.View.Input;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime.Input.LegacyInput
{
    [Serializable]
    public sealed class LegacyInputKey : ITerminalInputKeyMap<KeyCode>
    {
        [SerializeField] private KeyCode _openKeyCode = KeyCode.LeftBracket;
        [SerializeField] private KeyCode _closeKeyCode = KeyCode.Escape;
        [SerializeField] private KeyCode _executeKeyCode = KeyCode.Return;
        [SerializeField] private KeyCode _prevHistoryKeyCode = KeyCode.UpArrow;
        [SerializeField] private KeyCode _nextHistoryKeyCode = KeyCode.DownArrow;
        [SerializeField] private KeyCode _autocompleteKeyCode = KeyCode.Tab;

        public KeyCode GetKey(TerminalAction action) => action switch
        {
            TerminalAction.None => KeyCode.None,
            TerminalAction.Open => _openKeyCode,
            TerminalAction.Close => _closeKeyCode,
            TerminalAction.Execute => _executeKeyCode,
            TerminalAction.PreviousHistory => _prevHistoryKeyCode,
            TerminalAction.NextHistory => _nextHistoryKeyCode,
            TerminalAction.Autocomplete => _autocompleteKeyCode,
            TerminalAction.Focus => KeyCode.None,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
#endif

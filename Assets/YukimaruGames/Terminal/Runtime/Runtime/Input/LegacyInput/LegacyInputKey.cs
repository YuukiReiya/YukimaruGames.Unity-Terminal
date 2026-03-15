#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_LEGACY_INPUT_MANAGER
#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using UnityEngine;
using YukimaruGames.Terminal.UI;
using YukimaruGames.Terminal.UI.Input;

namespace YukimaruGames.Terminal.Runtime.Input.LegacyInput
{
    [Serializable]
    public sealed class LegacyInputKey : IInputKeyMap<KeyCode>
    {
        [SerializeField] private KeyCode _openKeyCode = KeyCode.LeftBracket;
        [SerializeField] private KeyCode _closeKeyCode = KeyCode.Escape;
        [SerializeField] private KeyCode _executeKeyCode = KeyCode.Return;
        [SerializeField] private KeyCode _prevHistoryKeyCode = KeyCode.UpArrow;
        [SerializeField] private KeyCode _nextHistoryKeyCode = KeyCode.DownArrow;
        [SerializeField] private KeyCode _autocompleteKeyCode = KeyCode.Tab;
        [SerializeField] private KeyCode _focusKeyCode = KeyCode.LeftControl;

        public KeyCode GetKey(Trigger action) => action switch
        {
            Trigger.None => KeyCode.None,
            Trigger.Open => _openKeyCode,
            Trigger.Close => _closeKeyCode,
            Trigger.Execute => _executeKeyCode,
            Trigger.PreviousHistory => _prevHistoryKeyCode,
            Trigger.NextHistory => _nextHistoryKeyCode,
            Trigger.Autocomplete => _autocompleteKeyCode,
            Trigger.Focus => _focusKeyCode,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}
#endif

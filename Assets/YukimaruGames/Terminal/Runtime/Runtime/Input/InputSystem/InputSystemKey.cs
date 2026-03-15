using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.UI;
using YukimaruGames.Terminal.UI.Input;

// ReSharper disable InconsistentNaming

namespace YukimaruGames.Terminal.Runtime.Input.InputSystem
{
    [Serializable]
    public sealed class InputSystemKey : IInputKeyMap<Key>
    {
        [SerializeField] private Key _openKey = Key.LeftBracket;
        [SerializeField] private Key _closeKey = Key.Escape;
        [SerializeField] private Key _executeKey = Key.Enter;
        [SerializeField] private Key _prevHistoryKey = Key.UpArrow;
        [SerializeField] private Key _nextHistoryKey = Key.DownArrow;
        [SerializeField] private Key _autocompleteKey = Key.Tab;
        [SerializeField] private Key _focusKey = Key.LeftCtrl;

        public Key GetKey(Trigger action) => action switch
        {
            Trigger.None => Key.None,
            Trigger.Open => _openKey,
            Trigger.Close => _closeKey,
            Trigger.Execute => _executeKey,
            Trigger.PreviousHistory => _prevHistoryKey,
            Trigger.NextHistory => _nextHistoryKey,
            Trigger.Autocomplete => _autocompleteKey,
            Trigger.Focus => _focusKey,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.UI.View.Input;
using YukimaruGames.Terminal.SharedKernel;

// ReSharper disable InconsistentNaming

namespace YukimaruGames.Terminal.Runtime.Input.InputSystem
{
    [Serializable]
    public sealed class InputSystemKey : ITerminalInputKeyMap<Key>
    {
        [SerializeField] private Key _openKey = Key.LeftBracket;
        [SerializeField] private Key _closeKey = Key.Escape;
        [SerializeField] private Key _executeKey = Key.Enter;
        [SerializeField] private Key _prevHistoryKey = Key.UpArrow;
        [SerializeField] private Key _nextHistoryKey = Key.DownArrow;
        [SerializeField] private Key _autocompleteKey = Key.Tab;
        [SerializeField] private Key _focusKey = Key.LeftCtrl;

        public Key GetKey(TerminalAction action) => action switch
        {
            TerminalAction.None => Key.None,
            TerminalAction.Open => _openKey,
            TerminalAction.Close => _closeKey,
            TerminalAction.Execute => _executeKey,
            TerminalAction.PreviousHistory => _prevHistoryKey,
            TerminalAction.NextHistory => _nextHistoryKey,
            TerminalAction.Autocomplete => _autocompleteKey,
            TerminalAction.Focus => _focusKey,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Presentation.Models.Event;
using YukimaruGames.Terminal.Presentation.Models.Input;

// ReSharper disable InconsistentNaming

namespace YukimaruGames.Terminal.Composition.Input.InputSystem
{
    [Serializable]
    public sealed class InputSystemKey : IInputKeyMap<Key>
    {
        [SerializeField] private Key _openKey = Key.LeftBracket;
        [SerializeField] private Key _closeKey = Key.Escape;
        [SerializeField] private Key _executeKey = Key.Enter;
        [SerializeField] private Key _cancelKey = Key.C;
        [SerializeField] private Key _cancelModifierKey = Key.LeftCtrl;
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
            TerminalAction.Cancel => _cancelKey,
            TerminalAction.PreviousHistory => _prevHistoryKey,
            TerminalAction.NextHistory => _nextHistoryKey,
            TerminalAction.Autocomplete => _autocompleteKey,
            TerminalAction.Focus => _focusKey,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        /// <inheritdoc/>
        public Key GetModifier(TerminalAction action) => action switch
        {
            TerminalAction.Cancel => _cancelModifierKey,
            _ => Key.None
        };
    }
}

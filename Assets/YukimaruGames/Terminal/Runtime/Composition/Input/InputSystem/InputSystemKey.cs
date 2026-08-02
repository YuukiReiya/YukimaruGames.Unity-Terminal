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
        [SerializeField] private Key _openModifierKey = Key.None;
        [SerializeField] private Key _closeKey = Key.Escape;
        [SerializeField] private Key _closeModifierKey = Key.None;
        [SerializeField] private Key _executeKey = Key.Enter;
        [SerializeField] private Key _executeModifierKey = Key.None;
        [SerializeField] private Key _cancelKey = Key.C;
        [SerializeField] private Key _cancelModifierKey = Key.LeftCtrl;
        [SerializeField] private Key _prevHistoryKey = Key.UpArrow;
        [SerializeField] private Key _prevHistoryModifierKey = Key.None;
        [SerializeField] private Key _nextHistoryKey = Key.DownArrow;
        [SerializeField] private Key _nextHistoryModifierKey = Key.None;
        [SerializeField] private Key _autocompleteKey = Key.Tab;
        [SerializeField] private Key _autocompleteModifierKey = Key.None;
        [SerializeField] private Key _focusKey = Key.LeftCtrl;
        [SerializeField] private Key _focusModifierKey = Key.None;

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
        /// <remarks>アクションごとに任意の修飾キーを設定できる(既定は<see cref="Key.None"/> = 修飾キー不要).</remarks>
        public Key GetModifier(TerminalAction action) => action switch
        {
            TerminalAction.None => Key.None,
            TerminalAction.Open => _openModifierKey,
            TerminalAction.Close => _closeModifierKey,
            TerminalAction.Execute => _executeModifierKey,
            TerminalAction.Cancel => _cancelModifierKey,
            TerminalAction.PreviousHistory => _prevHistoryModifierKey,
            TerminalAction.NextHistory => _nextHistoryModifierKey,
            TerminalAction.Autocomplete => _autocompleteModifierKey,
            TerminalAction.Focus => _focusModifierKey,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}

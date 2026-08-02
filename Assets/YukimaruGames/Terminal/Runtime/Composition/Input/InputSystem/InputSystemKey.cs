using System;
using System.Collections.Generic;
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
        [SerializeField] private Key[] _openModifierKeys = Array.Empty<Key>();
        [SerializeField] private Key _closeKey = Key.Escape;
        [SerializeField] private Key[] _closeModifierKeys = Array.Empty<Key>();
        [SerializeField] private Key _executeKey = Key.Enter;
        [SerializeField] private Key[] _executeModifierKeys = Array.Empty<Key>();
        [SerializeField] private Key _cancelKey = Key.C;
        [SerializeField] private Key[] _cancelModifierKeys = { Key.LeftCtrl };
        [SerializeField] private Key _prevHistoryKey = Key.UpArrow;
        [SerializeField] private Key[] _prevHistoryModifierKeys = Array.Empty<Key>();
        [SerializeField] private Key _nextHistoryKey = Key.DownArrow;
        [SerializeField] private Key[] _nextHistoryModifierKeys = Array.Empty<Key>();
        [SerializeField] private Key _autocompleteKey = Key.Tab;
        [SerializeField] private Key[] _autocompleteModifierKeys = Array.Empty<Key>();
        [SerializeField] private Key _focusKey = Key.LeftCtrl;
        [SerializeField] private Key[] _focusModifierKeys = Array.Empty<Key>();

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
        /// <remarks>アクションごとに任意個の修飾キーを設定できる(既定は空 = 修飾キー不要).</remarks>
        public IReadOnlyList<Key> GetModifiers(TerminalAction action) => action switch
        {
            TerminalAction.None => Array.Empty<Key>(),
            TerminalAction.Open => _openModifierKeys,
            TerminalAction.Close => _closeModifierKeys,
            TerminalAction.Execute => _executeModifierKeys,
            TerminalAction.Cancel => _cancelModifierKeys,
            TerminalAction.PreviousHistory => _prevHistoryModifierKeys,
            TerminalAction.NextHistory => _nextHistoryModifierKeys,
            TerminalAction.Autocomplete => _autocompleteModifierKeys,
            TerminalAction.Focus => _focusModifierKeys,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}

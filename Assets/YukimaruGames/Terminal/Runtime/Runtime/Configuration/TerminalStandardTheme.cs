using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable]
    public class TerminalStandardTheme : ITerminalTheme
    {
        [Header("View Settings")]
        [SerializeField] private Font _font;
        [SerializeField] private int _fontSize = 55;
        [SerializeField] private Color _backgroundColor = Color.black;
        [SerializeField] private Color _messageColor = Color.white;
        [SerializeField] private Color _entryColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private Color _assertColor = Color.red;
        [SerializeField] private Color _exceptionColor = Color.red;
        [SerializeField] private Color _systemColor = Color.white;
        [SerializeField] private Color _inputColor = new(0f, 1f, 0.3f);
        [SerializeField] private Color _caretColor = new(0f, 1f, 0.8f);
        [SerializeField] private Color _selectionColor = new(1f, 0.5f, 0f);
        [SerializeField] private Color _promptColor = new(0f, 0.8f, 0.15f);
        [SerializeField] private Color _executeButtonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _buttonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _copyButtonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private float _cursorFlashSpeed = 1.886792f;

        [Header("Window Settings")]
        [SerializeField] private TerminalState _bootupWindowState = TerminalState.Close;
        [SerializeField] private TerminalAnchor _anchor = TerminalAnchor.Top;
        [SerializeField] private TerminalWindowStyle _windowStyle = TerminalWindowStyle.Compact;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _compactScale = 0.35f;

        public Font Font => _font;
        public int FontSize => _fontSize;
        public Color BackgroundColor => _backgroundColor;
        public Color MessageColor => _messageColor;
        public Color EntryColor => _entryColor;
        public Color WarningColor => _warningColor;
        public Color ErrorColor => _errorColor;
        public Color AssertColor => _assertColor;
        public Color ExceptionColor => _exceptionColor;
        public Color SystemColor => _systemColor;
        public Color InputColor => _inputColor;
        public Color CaretColor => _caretColor;
        public Color SelectionColor => _selectionColor;
        public Color PromptColor => _promptColor;
        public Color ExecuteButtonColor => _executeButtonColor;
        public Color ButtonColor => _buttonColor;
        public Color CopyButtonColor => _copyButtonColor;
        public float CursorFlashSpeed => _cursorFlashSpeed;
        
        public TerminalState BootupWindowState => _bootupWindowState;
        public TerminalAnchor Anchor => _anchor;
        public TerminalWindowStyle WindowStyle => _windowStyle;
        public float Duration => _duration;
        public float CompactScale => _compactScale;
    }
}

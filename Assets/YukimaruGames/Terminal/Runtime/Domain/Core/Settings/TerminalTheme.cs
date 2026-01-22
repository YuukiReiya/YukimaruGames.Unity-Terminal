using System;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <inheritdoc cref="ITerminalTheme" />
    /// <remarks>
    /// UnityのInspectorで設定可能なターミナルの外観実装です。
    /// </remarks>
    [Serializable]
    public sealed class TerminalTheme : ITerminalTheme, IButtonThemeProvider
    {
        [SerializeField] private Font _font;
        [SerializeField, Min(1)] private int _fontSize = 55;
        [SerializeField] private Color _backgroundColor = Color.black;
        [SerializeField] private Color _messageColor = Color.white;
        [SerializeField] private Color _entryColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private Color _assertColor = Color.red;
        [SerializeField] private Color _exceptionColor = Color.red;
        [SerializeField] private Color _systemColor = Color.white;
        [SerializeField] private Color _inputColor = new(0f, 1f, 0.3f);
        [SerializeField] private Color _caretColor = Color.white;
        [SerializeField] private Color _selectionColor = new(1f, 0.5f, 0f);
        [SerializeField] private Color _promptColor = new(0f, 0.8f, 0.15f);
        [SerializeField, Min(0f)] private float _cursorFlashSpeed = 1.886792f;
        [SerializeField, Min(0f)] private float _duration = 1f;
        [SerializeField, Min(0f)] private float _compactScale = 0.35f;

        [Space]
        [Header("Button Theme")]
        [SerializeField] private TerminalButtonTheme _buttonTheme = new();

        /// <summary>
        /// ターミナルで使用するフォントを取得または設定します。
        /// </summary>
        /// <remarks>
        /// インターフェースには定義されていませんが、具象クラスでアセットを保持します。
        /// </remarks>
        public Font Font
        {
            get => _font;
            set => _font = value;
        }

        /// <inheritdoc />
        public int FontSize => _fontSize;

        /// <inheritdoc />
        public TerminalColor Background => ToTerminalColor(_backgroundColor);

        /// <inheritdoc />
        public TerminalColor Message => ToTerminalColor(_messageColor);

        /// <inheritdoc />
        public TerminalColor Entry => ToTerminalColor(_entryColor);

        /// <inheritdoc />
        public TerminalColor Warning => ToTerminalColor(_warningColor);

        /// <inheritdoc />
        public TerminalColor Error => ToTerminalColor(_errorColor);

        /// <inheritdoc />
        public TerminalColor Assert => ToTerminalColor(_assertColor);

        /// <inheritdoc />
        public TerminalColor Exception => ToTerminalColor(_exceptionColor);

        /// <inheritdoc />
        public TerminalColor System => ToTerminalColor(_systemColor);

        /// <inheritdoc />
        public TerminalColor Input => ToTerminalColor(_inputColor);

        /// <inheritdoc />
        public TerminalColor Caret => ToTerminalColor(_caretColor);

        /// <inheritdoc />
        public TerminalColor Selection => ToTerminalColor(_selectionColor);

        /// <inheritdoc />
        public TerminalColor Prompt => ToTerminalColor(_promptColor);

        /// <inheritdoc />
        public float CursorFlashSpeed => _cursorFlashSpeed;

        /// <inheritdoc />
        public float Duration => _duration;

        /// <inheritdoc />
        public float CompactScale => _compactScale;

        /// <inheritdoc />
        public ITerminalButtonTheme ButtonTheme => _buttonTheme;

        private static TerminalColor ToTerminalColor(Color color) => new(color.r, color.g, color.b, color.a);
    }
}

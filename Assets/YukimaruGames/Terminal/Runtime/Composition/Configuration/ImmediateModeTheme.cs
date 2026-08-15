using System;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// Immediate Mode(IMGUI)ベースの標準実装における<see cref="ITerminalTheme"/>実装.
    /// </summary>
    [Serializable, AddTypeMenu("IMGUI Theme")]
    public sealed class ImmediateModeTheme : ITerminalTheme
    {
        [Header("View Settings")]
        [SerializeField] private Font _font;

        /// <remarks>
        /// 1920x1080を基準にしたピクセル値。ウィンドウは画面高さの一定比率で開くため、
        /// 実質的にこの値が決めているのは「基準解像度のウィンドウ内に何行入るか」であり、
        /// 絶対的な読みやすさではない(1080pのウィンドウ高さ378pxに対し、55pxで約6行)。
        /// 小さくすると行数は増えるが、1行あたりのピクセルは減る。
        /// <para>
        /// この値は解像度に追従しない。uGUI版の<c>CanvasScaler</c>は
        /// <c>ConstantPixelSize</c>で拡大率も固定のため、解像度が下がるとウィンドウだけが
        /// 縮んでフォントサイズは据え置かれ、収まる行数が減る.
        /// </para>
        /// </remarks>
        [Tooltip("1920x1080を基準にしたフォントサイズ(px)。小さくするとウィンドウ内に入る行数が増える。")]
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
    }
}

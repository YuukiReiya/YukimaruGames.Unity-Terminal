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
        private const int DefaultReferenceWidth = 1920;
        private const int DefaultReferenceHeight = 1080;

        private const string ScaleFontWithScreenTooltip =
            "画面サイズに合わせてフォントサイズを拡縮する。オンにすると、どの解像度でもウィンドウに入る行数が一定になる。" +
            "オフの場合はSizeの値がそのまま描画サイズになる（従来の挙動）。";

        private const string ReferenceResolutionTooltip =
            "Sizeが想定している基準の解像度。この解像度のときSizeがそのまま使われる。" +
            "拡縮は高さに連動するため、現状は高さのみ参照する（Scale Font With Screenがオンのときのみ有効）。";

        // [Header]はDrawerが確保した矩形の外に描かれ、EditorGUI.GetPropertyHeightにも含まれない。
        // 矩形ベースのDrawer(ImmediateModeThemeDrawer)が独自に"Font"の見出しを描くため、
        // ここでの見出しは重複するうえ高さがずれる原因になる(#147).
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

        [Tooltip(ScaleFontWithScreenTooltip)]
        [SerializeField] private bool _scaleFontWithScreen;

        [Tooltip(ReferenceResolutionTooltip)]
        [SerializeField] private Vector2Int _referenceResolution = new(DefaultReferenceWidth, DefaultReferenceHeight);
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

        /// <inheritdoc/>
        public bool ScaleFontWithScreen => _scaleFontWithScreen;

        /// <inheritdoc/>
        public Vector2Int ReferenceResolution => _referenceResolution;
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

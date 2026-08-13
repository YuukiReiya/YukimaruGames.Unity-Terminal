#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Log;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Adapters.UIToolkit.Renderers
{
    /// <summary>
    /// UIToolkitによるログ表示の描画実装.
    /// </summary>
    /// <remarks>
    /// ログ行は<see cref="VisualElement"/>の行コンテナ(Label + 任意のコピー用ボタン)単位でプールし、
    /// <see cref="ScrollView"/>直下に必要数だけアタッチする.
    /// </remarks>
    public sealed class LogRenderer : ILogRenderer, IDisposable
    {
        private readonly ScrollView _scrollView;
        private readonly IClipboardRenderer _clipboardRenderer;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;
        private readonly ObjectPool<LogLineElement> _linePool;
        private readonly List<LogLineElement> _lineElements = new();
        private Color _copyButtonColor;
        private FontDefinition _fontDefinition;
        private int _fontSize;

        /// <summary>
        /// Inspector上のテーマ変更(<see cref="Composition.IInstaller.Resolve"/>経由の再同期)を
        /// 反映するための外部からのミューテーター.
        /// </summary>
        public Color CopyButtonColor
        {
            set => _copyButtonColor = value;
        }

        /// <summary>
        /// フォントが未割り当てのままだとグリフの計測ができずログ行の高さが常に0になり
        /// 一切表示されなくなる(#122で判明)ため、テーマのフォントを行要素にも反映する.
        /// </summary>
        public FontDefinition FontDefinition
        {
            set => _fontDefinition = value;
        }

        /// <summary>ログ行のフォントサイズ.</summary>
        public int FontSize
        {
            set => _fontSize = value;
        }

        public LogRenderer(
            ScrollView scrollView,
            IClipboardRenderer clipboardRenderer,
            IColorPaletteProvider colorPaletteProvider,
            ILauncherVisibleProvider launcherVisibleProvider,
            Color copyButtonColor)
        {
            _scrollView = scrollView;
            _clipboardRenderer = clipboardRenderer;
            _colorPaletteProvider = colorPaletteProvider;
            _launcherVisibleProvider = launcherVisibleProvider;
            _copyButtonColor = copyButtonColor;

            _linePool = new ObjectPool<LogLineElement>(
                createFunc: static () => new LogLineElement(),
                actionOnGet: null,
                actionOnRelease: static line => line.Reset(),
                actionOnDestroy: null,
                collectionCheck: true,
                defaultCapacity: 32,
                maxSize: 256);
        }

        private Color GetColor(MessageType type) => type switch
        {
            MessageType.Error => _colorPaletteProvider[Definitions.ThemeLabel.Error],
            MessageType.Assert => _colorPaletteProvider[Definitions.ThemeLabel.Assert],
            MessageType.Warning => _colorPaletteProvider[Definitions.ThemeLabel.Warning],
            MessageType.Message => _colorPaletteProvider[Definitions.ThemeLabel.Message],
            MessageType.Exception => _colorPaletteProvider[Definitions.ThemeLabel.Exception],
            MessageType.Entry => _colorPaletteProvider[Definitions.ThemeLabel.Entry],
            MessageType.System => _colorPaletteProvider[Definitions.ThemeLabel.System],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        /// <summary>
        /// ログ行の表示内容を<paramref name="data"/>の内容に同期する.
        /// </summary>
        public void Render(LogRenderData data)
        {
            if (_scrollView == null) return;

            SyncLineElements(data.LogRenderDataCollection.Count);

            var index = 0;
            foreach (var entry in data.LogRenderDataCollection)
            {
                // 差分に関わらず毎フレーム無条件にSetMessage/SetFont等を代入していると、
                // 変化の無い行でもテキストレイアウトがdirtyになり続ける。これ自体が末尾到達
                // 不能の主要因ではなかった(真因はScrollViewのflex-basis/min-height未指定による
                // 箱のはみ出しだった。WindowRoot.cs参照)が、無駄な再計測・再描画は避けるべき
                // ムダである(#122、Opus協力の上で確認)。実際に値が変化した行のみ再代入する.
                var line = _lineElements[index];
                line.Apply(entry, GetColor(entry.MessageType), _copyButtonColor, _fontDefinition, _fontSize,
                    ShouldDrawCopyButton(entry) ? _clipboardRenderer : null);

                ++index;
            }
        }

        private void SyncLineElements(int requiredCount)
        {
            while (_lineElements.Count < requiredCount)
            {
                var line = _linePool.Get();
                _scrollView.Add(line.Root);
                _lineElements.Add(line);
            }

            ReleaseLineElements(requiredCount);
        }

        private void ReleaseLineElements(int keepCount)
        {
            while (_lineElements.Count > keepCount)
            {
                var lastIndex = _lineElements.Count - 1;
                var line = _lineElements[lastIndex];
                line.Root.RemoveFromHierarchy();
                _linePool.Release(line);
                _lineElements.RemoveAt(lastIndex);
            }
        }

        private bool ShouldDrawCopyButton(LogEntry entry)
        {
            if (_launcherVisibleProvider is { IsVisible: false }) return false;
            if (string.IsNullOrWhiteSpace(entry.Message)) return false;

            return entry.MessageType switch
            {
                MessageType.System => false,
                _ => true,
            };
        }

        void IDisposable.Dispose()
        {
            ReleaseLineElements(0);
        }

        /// <summary>
        /// ログ1行分の表示要素(メッセージLabel + コピー用ボタン)をまとめて保持する.
        /// </summary>
        private sealed class LogLineElement
        {
            private readonly VisualElement _root;
            private readonly Label _label;
            private readonly Button _copyButton;

            private IClipboardRenderer _clipboardRenderer;
            private string _message;

            private string _appliedMessage;
            private Color _appliedColor;
            private Color _appliedCopyButtonColor;
            private FontDefinition _appliedFontDefinition;
            private int _appliedFontSize;
            private bool _appliedCopyButtonVisible;
            private bool _hasAppliedState;

            public VisualElement Root => _root;

            public LogLineElement()
            {
                _root = new VisualElement { name = "log-line" };
                _root.style.flexDirection = FlexDirection.Row;
                // flex-shrinkの既定値(1)のままだと、ScrollViewの表示領域を超えるログ行数に
                // なった際に各行が圧縮されて重なり合ってしまう(スクロールもできなくなる)。
                // 行の実測サイズを常に保つため、縮小させない.
                _root.style.flexShrink = 0;

                _label = new Label { name = "log-line-message" };
                _label.style.flexGrow = 1;
                _label.style.whiteSpace = WhiteSpace.Normal;

                _copyButton = new Button { text = "[COPY]", name = "log-line-copy-button" };
                // display:Noneではなくvisibility:Hiddenで隠す(Apply/Resetのコメント参照)。
                // レイアウト上の幅を保持したまま見た目だけ隠すことで、コピーボタンの出し分けが
                // Labelのavailable widthに影響しないようにする.
                _copyButton.style.visibility = Visibility.Hidden;
                _copyButton.pickingMode = PickingMode.Ignore;
                _copyButton.clicked += OnCopyButtonClicked;

                _root.Add(_label);
                _root.Add(_copyButton);

                // WhiteSpace.Normal(折り返しあり)のLabelの高さは、Yogaのmeasure functionによる
                // 自動計測に任せると、行の幅が変わるたび(ウィンドウリサイズ・FreeAspectでの
                // ゲームビュー可変幅等)に再計測が必要になり、1レイアウトパスで収束しないことがある。
                // これによりcontentContainerの高さがしばらく古いまま(実際の子要素の広がりより
                // 小さいまま)取り残され、末尾までスクロールできない・行同士が重なって表示される、
                // という2つの不具合が実機検証で確認された(#122、Opus協力の上で確認)。
                // Labelの幅が実際に確定・変化するたびに、MeasureTextSize()で折り返し後の高さを
                // 同期的に計測し、高さを明示指定することでこの収束待ちの経路自体を無くす.
                _label.RegisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
            }

            private void OnLabelGeometryChanged(GeometryChangedEvent evt)
            {
                if (Mathf.Approximately(evt.oldRect.width, evt.newRect.width)) return;
                ApplyMeasuredHeight();
            }

            private void ApplyMeasuredHeight()
            {
                if (string.IsNullOrEmpty(_label.text))
                {
                    _label.style.height = StyleKeyword.Auto;
                    return;
                }

                var style = _label.resolvedStyle;

                // resolvedStyle.widthはボーダーボックス幅だが、MeasureTextSize()は内容領域幅を
                // 期待する。TerminalWindow.ussはLabelにpadding/borderを指定していないため現状は
                // 差が出ないが、利用側のテーマ/StyleSheetでpadding・borderが付与されると、折り返し幅を
                // 過大に見積もり計測される高さが不足してテキストが欠ける(コードレビューで指摘・確認)。
                // ボーダーボックス幅からpadding/borderを差し引いた内容領域幅を計測に使う.
                var width = style.width
                    - style.paddingLeft - style.paddingRight
                    - style.borderLeftWidth - style.borderRightWidth;

                if (float.IsNaN(width) || width <= 0f)
                {
                    return;
                }

                var size = _label.MeasureTextSize(_label.text, width, VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
                _label.style.height = Mathf.Ceil(size.y);
            }

            /// <summary>
            /// 値が実際に変化した項目のみ書き込む。無条件に毎フレーム再代入すると変化の無い行でも
            /// テキストレイアウトがdirtyになり続け、無駄な再計測・再描画を招く(#122、Opus協力の
            /// 上で確認。マウスホイールで末尾に到達できない不具合自体の主因はこれではなく、
            /// ScrollViewのflex-basis/min-height未指定による箱のはみ出しだった).
            /// </summary>
            public void Apply(
                LogEntry entry,
                Color color,
                Color copyButtonColor,
                FontDefinition fontDefinition,
                int fontSize,
                IClipboardRenderer copyButtonClipboardRenderer)
            {
                _clipboardRenderer = copyButtonClipboardRenderer;
                _message = copyButtonClipboardRenderer != null ? entry.Message : _message;

                var copyButtonVisible = copyButtonClipboardRenderer != null;

                if (_hasAppliedState
                    && _appliedMessage == entry.Message
                    && _appliedColor == color
                    && _appliedCopyButtonColor == copyButtonColor
                    && Equals(_appliedFontDefinition, fontDefinition)
                    && _appliedFontSize == fontSize
                    && _appliedCopyButtonVisible == copyButtonVisible)
                {
                    return;
                }

                if (_appliedMessage != entry.Message)
                {
                    _label.text = entry.Message;
                    ApplyMeasuredHeight();
                }

                if (_appliedColor != color)
                {
                    _label.style.color = color;
                }

                if (_appliedCopyButtonColor != copyButtonColor)
                {
                    _copyButton.style.color = copyButtonColor;
                }

                if (!Equals(_appliedFontDefinition, fontDefinition) || _appliedFontSize != fontSize)
                {
                    _label.style.unityFontDefinition = fontDefinition;
                    _label.style.fontSize = fontSize;
                    _copyButton.style.unityFontDefinition = fontDefinition;
                    _copyButton.style.fontSize = fontSize;
                }

                if (_appliedCopyButtonVisible != copyButtonVisible)
                {
                    // display:None/Flexで出し分けると、コピーボタンの幅ぶんLabelのavailable widthが
                    // 変動し、WhiteSpace.Normalで折り返すLabelの高さ計測(Yogaのmeasure function)が
                    // 再計算される。行を大量追加するタイミングでこの幅変動が重なると、高さ計測が
                    // 1レイアウトパスで収束せず、contentContainerの高さ(≒verticalScroller.highValue
                    // の算出元)が実際の子要素の広がりより小さいまま取り残され、マウスホイールで末尾に
                    // 届かない・行同士が重なって表示される、という2つの不具合の共通原因になっていた
                    // (#122、Opus協力の上で確認)。visibility:Hidden(レイアウト上の幅は保持したまま
                    // 見た目だけ隠す)に切り替え、幅変動そのものを無くす.
                    _copyButton.style.visibility = copyButtonVisible ? Visibility.Visible : Visibility.Hidden;
                    _copyButton.pickingMode = copyButtonVisible ? PickingMode.Position : PickingMode.Ignore;
                }

                _appliedMessage = entry.Message;
                _appliedColor = color;
                _appliedCopyButtonColor = copyButtonColor;
                _appliedFontDefinition = fontDefinition;
                _appliedFontSize = fontSize;
                _appliedCopyButtonVisible = copyButtonVisible;
                _hasAppliedState = true;
            }

            public void Reset()
            {
                _label.text = string.Empty;
                _label.style.height = StyleKeyword.Auto;
                _message = string.Empty;
                _clipboardRenderer = null;
                _copyButton.style.visibility = Visibility.Hidden;
                _copyButton.pickingMode = PickingMode.Ignore;

                // 前回の適用値(_appliedXxx)をクリアしないと、プールから再利用された行が偶然
                // 前回と同じ値(例: 同一メッセージ)だった場合にApply()の差分ガードがtrueのまま
                // 誤判定し、実際にはReset()で空にした表示が復元されない(空行のまま・コピー
                // ボタンが表示されるべきなのに残ったまま等)不具合につながる(#122調査中に判明)。
                _appliedMessage = null;
                _appliedColor = default;
                _appliedCopyButtonColor = default;
                _appliedFontDefinition = default;
                _appliedFontSize = 0;
                _appliedCopyButtonVisible = false;
                _hasAppliedState = false;
            }

            private void OnCopyButtonClicked()
            {
                _clipboardRenderer?.Render(_message);
            }
        }
    }
}
#endif

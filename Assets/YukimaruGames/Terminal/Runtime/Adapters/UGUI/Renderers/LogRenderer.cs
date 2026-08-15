#if TERMINAL_UGUI_AVAILABLE
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Log;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Adapters.UGUI.Renderers
{
    /// <summary>
    /// ログ行を<see cref="ScrollRect"/>のContent配下へ並べて描画する.
    /// </summary>
    /// <remarks>
    /// 行のGameObjectは<see cref="ObjectPool{T}"/>で使い回す。行数が増減するたびに
    /// 生成・破棄を繰り返すとGC Allocとレイアウト再構築が嵩むため.
    /// </remarks>
    public sealed class LogRenderer : ILogRenderer, IDisposable
    {
        private readonly RectTransform _content;
        private readonly IClipboardRenderer _clipboardRenderer;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;
        private readonly ObjectPool<LogLineElement> _linePool;
        private readonly List<LogLineElement> _lineElements = new();

        private Color _copyButtonColor;
        private Font _font;
        private int _fontSize;

        /// <summary>コピーボタンの文字色.</summary>
        public Color CopyButtonColor
        {
            get => _copyButtonColor;
            set => _copyButtonColor = value;
        }

        /// <summary>
        /// ログ行のフォント.
        /// </summary>
        /// <remarks>
        /// <c>null</c>のままだと<see cref="Text"/>は文字を一切描画しない(#122の教訓)。
        /// 呼び出し側は<c>theme.Font</c>がnullのとき組み込みフォントへフォールバックすること.
        /// </remarks>
        public Font Font
        {
            get => _font;
            set => _font = value;
        }

        /// <summary>ログ行のフォントサイズ.</summary>
        public int FontSize
        {
            get => _fontSize;
            set => _fontSize = value;
        }

        public LogRenderer(
            RectTransform content,
            IClipboardRenderer clipboardRenderer,
            IColorPaletteProvider colorPaletteProvider,
            ILauncherVisibleProvider launcherVisibleProvider,
            Color copyButtonColor)
        {
            _content = content;
            _clipboardRenderer = clipboardRenderer;
            _colorPaletteProvider = colorPaletteProvider;
            _launcherVisibleProvider = launcherVisibleProvider;
            _copyButtonColor = copyButtonColor;

            _linePool = new ObjectPool<LogLineElement>(
                createFunc: () => new LogLineElement(_content),
                actionOnGet: static line => line.SetActive(true),
                actionOnRelease: static line => line.Reset(),
                actionOnDestroy: static line => line.Destroy());
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
        /// <remarks>
        /// 変化の無い行へ毎フレーム代入し続けるとレイアウトがdirtyになり続け、行高さの再計算が
        /// 収束しなくなる(#122)。実際に値が変化した行のみ再代入する(差分は各行が保持する).
        /// </remarks>
        public void Render(LogRenderData data)
        {
            if (_content == null) return;

            SyncLineElements(data.LogRenderDataCollection.Count);

            var index = 0;
            foreach (var entry in data.LogRenderDataCollection)
            {
                _lineElements[index].Apply(
                    entry,
                    GetColor(entry.MessageType),
                    _copyButtonColor,
                    _font,
                    _fontSize,
                    ShouldDrawCopyButton(entry) ? _clipboardRenderer : null);

                ++index;
            }
        }

        private void SyncLineElements(int requiredCount)
        {
            while (_lineElements.Count < requiredCount)
            {
                var line = _linePool.Get();
                line.SetAsLastSibling();
                _lineElements.Add(line);
            }

            ReleaseLineElements(requiredCount);
        }

        private void ReleaseLineElements(int keepCount)
        {
            while (_lineElements.Count > keepCount)
            {
                var lastIndex = _lineElements.Count - 1;
                _linePool.Release(_lineElements[lastIndex]);
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
            _linePool.Clear();
        }

        /// <summary>
        /// ログ1行分の表示要素(メッセージ<see cref="Text"/> + コピー用<see cref="Button"/>)を
        /// まとめて保持する.
        /// </summary>
        private sealed class LogLineElement
        {
            private const string LineName = "log-line";
            private const string MessageName = "log-line-message";
            private const string CopyButtonName = "log-line-copy-button";
            private const string CopyButtonText = "copy";

            private readonly RectTransform _root;
            private readonly Text _label;
            private readonly Button _copyButton;
            private readonly Graphic _copyButtonGraphic;
            private readonly Text _copyButtonLabel;

            private IClipboardRenderer _clipboardRenderer;
            private string _message;

            private string _appliedMessage;
            private Color _appliedColor;
            private Color _appliedCopyButtonColor;
            private Font _appliedFont;
            private int _appliedFontSize;
            private bool _appliedCopyButtonVisible;
            private bool _hasAppliedState;

            public LogLineElement(RectTransform parent)
            {
                _root = CreateElement(LineName, parent);
                var layout = _root.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                var fitter = _root.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _label = CreateText(MessageName, _root);
                var labelLayout = _label.gameObject.AddComponent<LayoutElement>();
                labelLayout.flexibleWidth = 1f;

                var copyButtonRoot = CreateElement(CopyButtonName, _root);
                _copyButtonGraphic = copyButtonRoot.gameObject.AddComponent<Image>();
                _copyButton = copyButtonRoot.gameObject.AddComponent<Button>();
                _copyButton.targetGraphic = _copyButtonGraphic;
                _copyButtonLabel = CreateText($"{CopyButtonName}-text", copyButtonRoot);
                _copyButtonLabel.text = CopyButtonText;
                _copyButtonLabel.alignment = TextAnchor.MiddleCenter;

                // クリック購読は生成時に1度だけ行う。Apply()のたびに登録すると多重発火する.
                _copyButton.onClick.AddListener(OnCopyButtonClicked);

                SetCopyButtonVisible(false);
            }

            public void SetActive(bool active) => _root.gameObject.SetActive(active);

            public void SetAsLastSibling() => _root.SetAsLastSibling();

            public void Apply(
                LogEntry entry,
                Color color,
                Color copyButtonColor,
                Font font,
                int fontSize,
                IClipboardRenderer clipboardRenderer)
            {
                _clipboardRenderer = clipboardRenderer;
                _message = entry.Message;

                var copyButtonVisible = clipboardRenderer != null;

                if (_hasAppliedState &&
                    _appliedMessage == entry.Message &&
                    _appliedColor == color &&
                    _appliedCopyButtonColor == copyButtonColor &&
                    _appliedFont == font &&
                    _appliedFontSize == fontSize &&
                    _appliedCopyButtonVisible == copyButtonVisible)
                {
                    return;
                }

                _label.text = entry.Message;
                _label.color = color;
                if (font != null) _label.font = font;
                if (fontSize > 0) _label.fontSize = fontSize;

                _copyButtonLabel.color = copyButtonColor;
                if (font != null) _copyButtonLabel.font = font;
                if (fontSize > 0) _copyButtonLabel.fontSize = fontSize;

                SetCopyButtonVisible(copyButtonVisible);

                _appliedMessage = entry.Message;
                _appliedColor = color;
                _appliedCopyButtonColor = copyButtonColor;
                _appliedFont = font;
                _appliedFontSize = fontSize;
                _appliedCopyButtonVisible = copyButtonVisible;
                _hasAppliedState = true;
            }

            /// <summary>
            /// プールへ返却する際に状態をリセットする.
            /// </summary>
            /// <remarks>
            /// 差分ガード用のキャッシュもクリアすること。残したままだと再利用時に
            /// 「前回と同じ値」と誤判定して表示が復元されない(#122の教訓).
            /// </remarks>
            public void Reset()
            {
                _clipboardRenderer = null;
                _message = null;

                _label.text = string.Empty;
                SetCopyButtonVisible(false);
                SetActive(false);

                _appliedMessage = null;
                _appliedColor = default;
                _appliedCopyButtonColor = default;
                _appliedFont = null;
                _appliedFontSize = 0;
                _appliedCopyButtonVisible = false;
                _hasAppliedState = false;
            }

            public void Destroy()
            {
                _copyButton.onClick.RemoveListener(OnCopyButtonClicked);

                if (_root == null) return;

                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_root.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_root.gameObject);
                }
            }

            /// <summary>
            /// コピーボタンの表示・非表示を切り替える.
            /// </summary>
            /// <remarks>
            /// <c>SetActive</c>で出し分けると<see cref="HorizontalLayoutGroup"/>の割り当て幅が
            /// 変わり、メッセージ側の折り返し位置と行高さが再計算されて収束しなくなる(#122の教訓)。
            /// レイアウト上の占有幅は保ったまま、描画と入力だけを止める.
            /// </remarks>
            private void SetCopyButtonVisible(bool visible)
            {
                _copyButtonGraphic.enabled = visible;
                _copyButtonLabel.enabled = visible;
                _copyButton.interactable = visible;
            }

            private void OnCopyButtonClicked() => _clipboardRenderer?.Render(_message);

            private static RectTransform CreateElement(string elementName, RectTransform parent)
            {
                var element = new GameObject(elementName, typeof(RectTransform));
                var rectTransform = (RectTransform)element.transform;
                rectTransform.SetParent(parent, false);
                return rectTransform;
            }

            private static Text CreateText(string elementName, RectTransform parent)
            {
                var element = CreateElement(elementName, parent);
                var text = element.gameObject.AddComponent<Text>();
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                return text;
            }
        }
    }
}
#endif

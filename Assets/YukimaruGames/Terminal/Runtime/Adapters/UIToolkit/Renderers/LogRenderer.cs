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

        public void Render(LogRenderData data)
        {
            if (_scrollView == null) return;

            SyncLineElements(data.LogRenderDataCollection.Count);

            var index = 0;
            foreach (var entry in data.LogRenderDataCollection)
            {
                var line = _lineElements[index];
                line.SetMessage(entry.Message);
                line.SetColor(GetColor(entry.MessageType));
                line.SetCopyButtonColor(_copyButtonColor);
                line.SetFont(_fontDefinition, _fontSize);

                if (ShouldDrawCopyButton(entry))
                {
                    line.ShowCopyButton(_clipboardRenderer, entry.Message);
                }
                else
                {
                    line.HideCopyButton();
                }

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
                _copyButton.style.display = DisplayStyle.None;
                _copyButton.clicked += OnCopyButtonClicked;

                _root.Add(_label);
                _root.Add(_copyButton);
            }

            public void SetMessage(string message)
            {
                _message = message;
                _label.text = message;
            }

            public void SetColor(Color color)
            {
                _label.style.color = color;
            }

            public void SetFont(FontDefinition fontDefinition, int fontSize)
            {
                _label.style.unityFontDefinition = fontDefinition;
                _label.style.fontSize = fontSize;
                _copyButton.style.unityFontDefinition = fontDefinition;
                _copyButton.style.fontSize = fontSize;
            }

            public void SetCopyButtonColor(Color color)
            {
                _copyButton.style.color = color;
            }

            public void ShowCopyButton(IClipboardRenderer clipboardRenderer, string copyText)
            {
                _clipboardRenderer = clipboardRenderer;
                _message = copyText;
                _copyButton.style.display = DisplayStyle.Flex;
            }

            public void HideCopyButton()
            {
                _copyButton.style.display = DisplayStyle.None;
            }

            public void Reset()
            {
                _label.text = string.Empty;
                _message = string.Empty;
                _clipboardRenderer = null;
                _copyButton.style.display = DisplayStyle.None;
            }

            private void OnCopyButtonClicked()
            {
                _clipboardRenderer?.Render(_message);
            }
        }
    }
}
#endif

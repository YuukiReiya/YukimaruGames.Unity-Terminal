using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Adapters.IMGUI.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Log;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Adapters.IMGUI.Renderers
{
    public sealed class LogRenderer : ILogRenderer, IDisposable
    {
        private readonly IClipboardRenderer _clipboardRenderer;
        private readonly IGUIStyleAccessor _styleAccessor;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly LogLinePool _linePool;
        private readonly List<LogLineView> _lineViews = new();

        public event Action<LogEntry> OnPreRender;
        public event Action<LogEntry> OnPostRender;

        public LogRenderer(
            IClipboardRenderer clipboardRenderer,
            IGUIStyleAccessor styleAccessor,
            IColorPaletteProvider colorPaletteProvider,
            LogLinePool linePool)
        {
            _clipboardRenderer = clipboardRenderer;
            _styleAccessor = styleAccessor;
            _colorPaletteProvider = colorPaletteProvider;
            _linePool = linePool;
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

        void IDisposable.Dispose()
        {
            OnPreRender = null;
            OnPostRender = null;

            ReleaseLineViews(0);
        }

        public void Render(LogRenderData data)
        {
            GUILayout.FlexibleSpace();
            var cursorColor = UnityEngine.GUI.skin.settings.cursorColor;

            try
            {
                UnityEngine.GUI.skin.settings.cursorColor = Color.clear;

                SyncLineViews(data.LogRenderDataCollection.Count);

                var index = 0;
                foreach (var renderData in data.LogRenderDataCollection)
                {
                    OnPreRender?.Invoke(renderData);

                    _styleAccessor.SetColor(GetColor(renderData.MessageType));
                    // TODO:コピペ可能な選択フィールドの実装が理想. (#102)
                    var lineView = _lineViews[index];
                    lineView.SetMessage(renderData.Message);
                    lineView.Render(_styleAccessor.GetStyle());
                    if (ShouldDrawCopyButton(renderData)) _clipboardRenderer.Render(renderData.Message);

                    OnPostRender?.Invoke(renderData);
                    ++index;
                }
            }
            finally
            {
                UnityEngine.GUI.skin.settings.cursorColor = cursorColor;
            }
        }

        /// <summary>
        /// 保持している<see cref="LogLineView"/>の件数を<paramref name="requiredCount"/>に合わせる.
        /// </summary>
        private void SyncLineViews(int requiredCount)
        {
            if (_linePool == null) return;

            while (_lineViews.Count < requiredCount)
            {
                _lineViews.Add(_linePool.Get());
            }

            ReleaseLineViews(requiredCount);
        }

        /// <summary>
        /// 保持している<see cref="LogLineView"/>を末尾から<paramref name="keepCount"/>件になるまでプールへ返却する.
        /// </summary>
        private void ReleaseLineViews(int keepCount)
        {
            if (_linePool == null) return;

            while (_lineViews.Count > keepCount)
            {
                var lastIndex = _lineViews.Count - 1;
                _linePool.Release(_lineViews[lastIndex]);
                _lineViews.RemoveAt(lastIndex);
            }
        }

        private bool ShouldDrawCopyButton(LogEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.Message))
            {
                return false;
            }

            return entry.MessageType switch
            {
                MessageType.System => false,
                _ => true,
            };
        }
    }
}

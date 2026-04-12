using System;
using UnityEngine;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Log;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Presentation.Renderers
{
    public sealed class LogRenderer : ILogRenderer, IDisposable
    {
        private readonly IClipboardRenderer _clipboardRenderer;
        private readonly IGUIStyleAccessor _styleAccessor;
        private readonly IColorPaletteProvider _colorPaletteProvider;

        public event Action<LogEntry> OnPreRender;
        public event Action<LogEntry> OnPostRender;

        public LogRenderer(IClipboardRenderer clipboardRenderer,IGUIStyleAccessor styleAccessor, IColorPaletteProvider colorPaletteProvider)
        {
            _clipboardRenderer = clipboardRenderer;
            _styleAccessor = styleAccessor;
            _colorPaletteProvider = colorPaletteProvider;
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
        }

        public void Render(LogRenderData data)
        {
            GUILayout.FlexibleSpace();
            var cursorColor = GUI.skin.settings.cursorColor;
            GUI.skin.settings.cursorColor = Color.clear;

            foreach (var renderData in data.LogRenderDataCollection)
            {
                OnPreRender?.Invoke(renderData);

                _styleAccessor.SetColor(GetColor(renderData.MessageType));
                // TODO:コピペ可能な選択フィールドの実装が理想.
                GUILayout.Label(renderData.Message, _styleAccessor.GetStyle());
                if (ShouldDrawCopyButton(renderData)) _clipboardRenderer.Render(renderData.Message);

                OnPostRender?.Invoke(renderData);
            }

            GUI.skin.settings.cursorColor = cursorColor;
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

using System;
using UnityEngine;
using YukimaruGames.Terminal.Application.Commands;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.UI.Core;

namespace YukimaruGames.Terminal.UI.Log
{
    public sealed class LogRenderer : ILogRenderer, IDisposable
    {
        private readonly IClipboardRenderer _clipboardRenderer;
        private readonly IGUIStyleProvider _styleProvider;
        private readonly IColorPaletteProvider _colorPaletteProvider;

        public event Action<LogEntry> OnPreRender;
        public event Action<LogEntry> OnPostRender;

        public LogRenderer(IClipboardRenderer clipboardRenderer, IGUIStyleProvider styleProvider, IColorPaletteProvider colorPaletteProvider)
        {
            _clipboardRenderer = clipboardRenderer;
            _styleProvider = styleProvider;
            _colorPaletteProvider = colorPaletteProvider;
        }

        private Color GetColor(MessageType type) => type switch
        {
            MessageType.Error => _colorPaletteProvider[Constants.ThemeLabel.Error],
            MessageType.Assert => _colorPaletteProvider[Constants.ThemeLabel.Assert],
            MessageType.Warning => _colorPaletteProvider[Constants.ThemeLabel.Warning],
            MessageType.Message => _colorPaletteProvider[Constants.ThemeLabel.Message],
            MessageType.Exception => _colorPaletteProvider[Constants.ThemeLabel.Exception],
            MessageType.Entry => _colorPaletteProvider[Constants.ThemeLabel.Entry],
            MessageType.System => _colorPaletteProvider[Constants.ThemeLabel.System],
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public void Dispose()
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

                _styleProvider.SetColor(GetColor(renderData.MessageType));
                // TODO:コピペ可能な選択フィールドの実装が理想.
                GUILayout.Label(renderData.Message, _styleProvider.GetStyle());
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

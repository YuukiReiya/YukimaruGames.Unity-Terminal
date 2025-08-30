using System;
using UnityEngine;
using YukimaruGames.Terminal.Application.Model;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using ColorPalette=YukimaruGames.Terminal.SharedKernel.Constants.Constants.ColorPalette;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalLogRenderer : ITerminalLogRenderer, IDisposable
    {
        private readonly IGUIStyleProvider _styleProvider;
        private readonly IColorPaletteProvider _colorPaletteProvider;

        public event Action<LogRenderData> OnPreRender;
        public event Action<LogRenderData> OnPostRender;

        public TerminalLogRenderer(IGUIStyleProvider styleProvider, IColorPaletteProvider colorPaletteProvider)
        {
            _styleProvider = styleProvider;
            _colorPaletteProvider = colorPaletteProvider;
        }

        private Color GetColor(MessageType type) => type switch
        {
            MessageType.Error => _colorPaletteProvider.GetColor(ColorPalette.Error),
            MessageType.Assert => _colorPaletteProvider.GetColor(ColorPalette.Assert),
            MessageType.Warning => _colorPaletteProvider.GetColor(ColorPalette.Warning),
            MessageType.Message => _colorPaletteProvider.GetColor(ColorPalette.Message),
            MessageType.Exception => _colorPaletteProvider.GetColor(ColorPalette.Exception),
            MessageType.Entry => _colorPaletteProvider.GetColor(ColorPalette.Entry),
            MessageType.System => _colorPaletteProvider.GetColor(ColorPalette.System),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public void Dispose()
        {
            OnPreRender = null;
            OnPostRender = null;
        }

        public void Render(TerminalLogRenderData data)
        {
            GUILayout.FlexibleSpace();
            var cursorColor = GUI.skin.settings.cursorColor;
            GUI.skin.settings.cursorColor = Color.clear;

            foreach (var logEntry in data.LogRenderDataCollection)
            {
                OnPreRender?.Invoke(logEntry);

                _styleProvider.SetColor(GetColor(logEntry.MessageType));
                // TODO:コピペ可能な選択フィールドの実装が理想.
                GUILayout.Label(logEntry.Message, _styleProvider.GetStyle());
                
                OnPostRender?.Invoke(logEntry);
            }

            GUI.skin.settings.cursorColor = cursorColor;
        }
    }
}

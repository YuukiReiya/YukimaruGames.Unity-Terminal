using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalLogCopyButtonRenderer : ITerminalLogCopyButtonRenderer, IDisposable
    {
        private readonly ITerminalButtonVisibleProvider _buttonVisibleButtonVisibleProvider;
        private readonly IGUIStyleProvider _styleProvider;

        private const string DisplayText = "📋";//"🔗"

        public event Action<string> OnClickButton;
        
        public TerminalLogCopyButtonRenderer(ITerminalButtonVisibleProvider buttonVisibleProvider,IGUIStyleProvider styleProvider)
        {
            _buttonVisibleButtonVisibleProvider = buttonVisibleProvider;
            _styleProvider = styleProvider;
        }

        public void Render(string copyText)
        {
            if (!_buttonVisibleButtonVisibleProvider.IsVisible) return;

            if (GUILayout.Button(DisplayText, _styleProvider.GetStyle()))
            {
                OnClickButton?.Invoke(copyText);
            }
        }
        
        public void Dispose()
        {
            OnClickButton = null;
        }
    }
}

using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Launcher;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.Clipboard
{
    public sealed class ClipboardRenderer : IClipboardRenderer, IDisposable
    {
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;
        private readonly IGUIStyleProvider _styleProvider;

        private const string DisplayText = "📋";//"🔗"

        public event Action<string> OnClickButton;
        
        public ClipboardRenderer(ILauncherVisibleProvider launcherVisibleProvider,IGUIStyleProvider styleProvider)
        {
            _launcherVisibleProvider = launcherVisibleProvider;
            _styleProvider = styleProvider;
        }

        public void Render(string copyText)
        {
            if (!_launcherVisibleProvider.IsVisible) return;

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

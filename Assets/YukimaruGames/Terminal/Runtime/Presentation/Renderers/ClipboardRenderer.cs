using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Presentation.Renderers
{
    public sealed class ClipboardRenderer : IClipboardRenderer, IDisposable
    {
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;
        private readonly IGUIStyleProvider _styleProvider;

        private const string DisplayText = "[COPY]";

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
        
        void IDisposable.Dispose()
        {
            OnClickButton = null;
        }
    }
}

using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalPromptRenderer : ITerminalPromptRenderer,IDisposable
    {
        private readonly IGUIStyleProvider _provider;
        private Vector2 _promptSize;

        private string _prompt= "$";
        public string Prompt
        {
            private get => _prompt;
            set
            {
                if (_prompt == value) return;
                _prompt = value;
                _promptSize = CalcSize(_provider, value);
            }
        } 

        public TerminalPromptRenderer(IGUIStyleProvider provider)
        {
            _provider = provider;
            _promptSize = CalcSize(_provider, _prompt);
            _provider.OnStyleChanged += OnChangedStyle;
        }
        
        public void Dispose()
        {
            if (_provider != null)
            {
                _provider.OnStyleChanged -= OnChangedStyle;
            }
        }

        public void Render()
        {
            if (string.IsNullOrWhiteSpace(Prompt)) return;
            GUILayout.Label(Prompt, _provider.GetStyle(), GUILayout.Width(_promptSize.x), GUILayout.Height(_promptSize.y));
        }

        private Vector2 CalcSize(IGUIStyleProvider provider,string prompt) => provider?.GetStyle().CalcSize(new GUIContent(prompt)) ?? Vector2.zero;

        private void OnChangedStyle()
        {
            _promptSize = CalcSize(_provider, Prompt);
        }
    }
}

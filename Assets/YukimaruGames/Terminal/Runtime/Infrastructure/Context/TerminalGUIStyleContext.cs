using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class TerminalGUIStyleContext : IGUIStyleProvider, IDisposable
    {
        private readonly IFontProvider _provider;
        private readonly Lazy<GUIStyle> _styleLazy;
        private readonly RectOffset _padding = new();

        public event Action OnStyleChanged;

        public TerminalGUIStyleContext(IFontProvider provider)
        {
            _provider = provider;
            _styleLazy = new Lazy<GUIStyle>(() => new GUIStyle
            {
                padding = _padding,
                normal = new GUIStyleState
                {
                    textColor = Color.white,
                },
                font = _provider.Font,
                fontSize = _provider.Size,
                wordWrap = true,
            });

            _provider.OnSizeChanged += HandleFontSizeChanged;
        }

        public GUIStyle GetStyle() => _styleLazy.Value;

        public void SetPadding(int padding)
        {
            if (_padding.left == padding &&
                _padding.right == padding &&
                _padding.top == padding &&
                _padding.bottom == padding)
            {
                return;
            }

            _padding.left = padding;
            _padding.right = padding;
            _padding.bottom = padding;
            _padding.top = padding;

            OnStyleChanged?.Invoke();
        }

        public void SetColor(Color color)
        {
            if (_styleLazy.Value.normal.textColor == color) return;

            _styleLazy.Value.normal.textColor = color;
            OnStyleChanged?.Invoke();
        }

        private void HandleFontSizeChanged(int size) => _styleLazy.Value.fontSize = size;

        void IDisposable.Dispose()
        {
            OnStyleChanged = null;
            if (_provider != null)
            {
                _provider.OnSizeChanged -= HandleFontSizeChanged;
            }
        }
    }
}

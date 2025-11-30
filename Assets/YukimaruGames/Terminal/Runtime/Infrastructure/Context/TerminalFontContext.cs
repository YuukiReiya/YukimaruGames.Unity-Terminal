using System;
using UnityEngine;
using YukimaruGames.Terminal.Runtime.Shared.Extensions;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class TerminalFontContext : IFontProvider, IFontConfigurator,IDisposable
    {
        private Font _font;
        private int _size;

        public Font Font
        {
            get => _font;
            set => SetFont(value);
        }

        public int Size
        {
            get => _size;
            set => SetSize(value);
        }

        public event Action OnFontChanged;
        public event Action<int> OnSizeChanged;

        public TerminalFontContext(Font font = null)
        {
            if (font != null)
            {
                _font = font;
                _size = font.fontSize;
            }
            else
            {
                const int size = 14;
                _font = Font.CreateDynamicFontFromOSFont("Courier New", size);
                _size = size;
            }

            if (_font != null) return;
            
            _font = Font.CreateDynamicFontFromOSFont("Arial", _size);
            Debug.LogWarning("Font 'Courier New' not found. Falling back to Arial.");
        }

        private void SetFont(Font font)
        {
            if (font == null || _font == font) return;

            Clear();

            _font = font;
            OnFontChanged?.Invoke();
        }
        
        private void SetSize(int size)
        {
            if (_size == size || size <= 0) return;

            _size = size;
            OnSizeChanged?.Invoke(size);
        }
        
        private void Clear()
        {
            if (_font == null) return;

            _font.Destroy();
            _font = null;
        }

        public void Dispose()
        {
            Clear();
            OnFontChanged = null;
            OnSizeChanged = null;
        }
    }
}

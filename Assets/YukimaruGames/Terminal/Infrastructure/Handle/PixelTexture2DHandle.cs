using System;
using UnityEngine;
using YukimaruGames.Terminal.Runtime.Shared.Extensions;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class PixelTexture2DHandle : IDisposable
    {
        private Texture2D _content;
        private const int Width = 1;
        private const int Height = 1;

        public PixelTexture2DHandle(Color color)
        {
            Create(color);
        }
        
        public void Dispose()
        {
            Release();
        }
        
        private void Create(in Color color)
        {
            _content = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            SetColor(color);
        }
        
        private void Release()
        {
            _content.Destroy();
            _content = null;
        }

        public Texture2D GetTexture2D() => _content;
        
        public void SetColor(in Color color)
        {
            if (_content == null) return;
            
            _content.SetPixel(0, 0, color);
            _content.Apply();
        }
    }
}

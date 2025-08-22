using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class PixelTexture2DRepository : IPixelTexture2DRepository, IDisposable
    {
        private readonly Dictionary<string, PixelTexture2DHandle> _dic = new();

        public void Add(string key, Color color)
        {
            _dic.TryAdd(key, new PixelTexture2DHandle(color));
        }

        public Texture2D GetTexture2D(string key)
        {
            return _dic.GetValueOrDefault(key)?.GetTexture2D();
        }

        public void SetColor(string key, in Color color)
        {
            _dic.GetValueOrDefault(key)?.SetColor(color);
        }

        public void Dispose()
        {
            foreach (var kvp in _dic)
            {
                kvp.Value?.Dispose();
            }

            _dic.Clear();
        }
    }
}

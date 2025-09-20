using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class PixelTexture2DRepository : IPixelTexture2DRepository, IDisposable
    {
        private readonly Dictionary<string, PixelTexture2DHandle> _dic = new();

        Texture2D IPixelTexture2DRepository.GetTexture2D(string key)
        {
            return _dic.GetValueOrDefault(key)?.GetTexture2D();
        }

        void IPixelTexture2DRepository.SetColor(string key, in Color color)
        {
            if (_dic.TryGetValue(key,out var handle))
            {
                handle.SetColor(color);
            }
            else
            {
                _dic.Add(key, new PixelTexture2DHandle(color));
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var kvp in _dic)
            {
                kvp.Value?.Dispose();
            }

            _dic.Clear();
        }
    }
}

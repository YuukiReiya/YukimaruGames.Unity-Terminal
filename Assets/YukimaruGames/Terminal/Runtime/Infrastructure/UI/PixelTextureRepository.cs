using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Repositories;

namespace YukimaruGames.Terminal.Infrastructure.UI
{
    public sealed class PixelTextureRepository : IPixelTextureRepository, IDisposable
    {
        private readonly Dictionary<string, PixelTextureHandle> _dic = new();

        public void Add(string key, Color color)
        {
            _dic.TryAdd(key, new PixelTextureHandle(color));
        }

        public Texture2D GetTexture2D(string key)
        {
            return _dic.GetValueOrDefault(key)?.GetTexture2D();
        }

        public void SetColor(string key, in Color color)
        {
            if (_dic.TryGetValue(key,out var handle))
            {
                handle.SetColor(color);
            }
            else
            {
                _dic.Add(key, new PixelTextureHandle(color));
            }
        }

        void IDisposable.Dispose()
        {
            foreach (var kvp in _dic)
            {
                if (kvp.Value is IDisposable handle)
                {
                    handle.Dispose();
                }
            }

            _dic.Clear();
        }
    }
}

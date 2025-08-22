using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class ColorPaletteProvider : IColorPaletteProvider
    {
        private readonly Dictionary<string, Color> _map = new();

        public ColorPaletteProvider(IReadOnlyDictionary<string, Color> map)
        {
            _map.Clear();
            foreach (var kvp in map) _map[kvp.Key] = kvp.Value;
        }
        
        public Color GetColor(string key) => _map[key];

        public void SetColor(string key, Color color)
        {
            _map[key] = color;
        }
    }
}

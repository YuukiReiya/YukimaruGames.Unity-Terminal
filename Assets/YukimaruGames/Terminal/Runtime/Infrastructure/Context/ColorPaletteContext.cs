using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class ColorPaletteContext : IColorPaletteProvider, IColorPaletteMutator
    {
        private readonly Dictionary<string, Color> _map = new();

        public ColorPaletteContext(IReadOnlyDictionary<string, Color> map)
        {
            _map.Clear();
            foreach (var kvp in map) _map[kvp.Key] = kvp.Value;
        }

        Color IColorPaletteProvider.GetColor(string key) => _map[key];

        void IColorPaletteMutator.SetColor(string key, Color color) => _map[key] = color;
    }
}

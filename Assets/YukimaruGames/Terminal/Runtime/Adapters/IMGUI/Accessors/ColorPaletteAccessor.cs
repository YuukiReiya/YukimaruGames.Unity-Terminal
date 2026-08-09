using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;

namespace YukimaruGames.Terminal.Adapters.IMGUI.Accessors
{
    /// <summary>
    /// テーマラベルとカラーの対応表を保持するアクセサ.
    /// </summary>
    public sealed class ColorPaletteAccessor : IColorPaletteAccessor
    {
        private readonly Dictionary<string, Color> _map = new();

        public ColorPaletteAccessor(IReadOnlyDictionary<string, Color> map)
        {
            _map.Clear();
            foreach (var kvp in map) _map[kvp.Key] = kvp.Value;
        }

        public Color this[string key]
        {
            get => _map[key];
            set => _map[key] = value;
        }
    }
}

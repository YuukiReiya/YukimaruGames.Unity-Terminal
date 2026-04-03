using UnityEngine;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface IColorPaletteProvider
    {
        Color this[string key] { get; }
    }
}

using UnityEngine;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface IColorPaletteProvider
    {
        Color GetColor(string key);
    }
}

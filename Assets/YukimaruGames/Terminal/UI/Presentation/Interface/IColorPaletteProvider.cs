using UnityEngine;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface IColorPaletteProvider
    {
        Color GetColor(string key);
    }
}

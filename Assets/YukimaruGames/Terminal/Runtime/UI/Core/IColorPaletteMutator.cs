using UnityEngine;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface IColorPaletteMutator
    {
        Color this[string key] { set; }
    }
}

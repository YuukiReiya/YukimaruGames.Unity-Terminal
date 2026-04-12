using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface IColorPaletteAccessor :
        IColorPaletteMutator,
        IColorPaletteProvider
    {
        new Color this[string key] { get; set; }
    }

    public interface IColorPaletteMutator
    {
        Color this[string key] { set; }
    }

    public interface IColorPaletteProvider
    {
        Color this[string key] { get; }
    }
}
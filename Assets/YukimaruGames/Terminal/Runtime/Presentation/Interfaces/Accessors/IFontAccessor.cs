using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface IFontAccessor :
        IFontMutator,
        IFontProvider
    {
        new Font Font { get; set; }
        new int Size { get; set; }
    }

    public interface IFontMutator
    {
        Font Font { set; }
        int Size { set; }
    }
    
    public interface IFontProvider
    {
        Font Font { get; }
        int Size { get; }
        event Action OnFontChanged;
        event Action<int> OnSizeChanged;
    }
}

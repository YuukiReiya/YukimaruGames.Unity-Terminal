using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface IFontProvider
    {
        Font Font { get; }
        int Size { get; }
        event Action OnFontChanged;
        event Action<int> OnSizeChanged;
    }
}

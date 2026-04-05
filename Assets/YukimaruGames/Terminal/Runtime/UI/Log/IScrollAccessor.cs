using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Log
{
    public interface IScrollAccessor :
        IScrollProvider,
        IScrollMutator
    {
        new Vector2 ScrollPosition { get; set; }
    }

    public interface IScrollMutator
    {
        Vector2 ScrollPosition { set; }
        void ScrollToEnd();
    }
    
    public interface IScrollProvider
    {
        Vector2 ScrollPosition { get; }
        event Action<Vector2> OnScrollChanged;
    }
}

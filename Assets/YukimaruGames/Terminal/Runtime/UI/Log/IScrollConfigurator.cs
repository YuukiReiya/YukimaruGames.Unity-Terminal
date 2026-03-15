using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Log
{
    public interface IScrollConfigurator
    {
        Vector2 ScrollPosition { get; set; }
        event Action<Vector2> OnScrollChanged;
        void ScrollToEnd();
    }
}

using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface IGUIStyleProvider
    {
        GUIStyle GetStyle();
        event Action OnStyleChanged;
    }
}

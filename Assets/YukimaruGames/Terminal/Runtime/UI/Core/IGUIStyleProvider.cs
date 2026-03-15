using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface IGUIStyleProvider
    {
        GUIStyle GetStyle();
        void SetPadding(int padding);
        void SetColor(Color color);
        event Action OnStyleChanged;
    }
}

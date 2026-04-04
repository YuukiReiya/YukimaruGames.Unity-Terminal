using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface IGUIStyleAccessor :
        IGUIStyleProvider,
        IGUIStyleMutator
    {
    
    }
    
    public interface IGUIStyleProvider
    {
        GUIStyle GetStyle();
        event Action OnStyleChanged;
    }
    
    public interface IGUIStyleMutator
    {
        void SetPadding(int padding);
        void SetColor(in Color color);
    }
}

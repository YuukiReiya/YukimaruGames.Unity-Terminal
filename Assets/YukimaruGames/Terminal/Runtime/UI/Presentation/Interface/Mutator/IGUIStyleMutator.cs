using UnityEngine;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface IGUIStyleMutator
    {
        void SetPadding(int padding);
        void SetColor(in Color color);
    }
}

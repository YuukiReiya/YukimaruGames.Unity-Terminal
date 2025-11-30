using UnityEngine;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface IGUIStyleConfigurator
    {
        void SetPadding(int padding);
        void SetColor(in Color color);
    }
}

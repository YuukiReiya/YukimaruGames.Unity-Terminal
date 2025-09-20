using UnityEngine;

namespace YukimaruGames.Terminal.UI.View
{
    public interface IPixelTexture2DRepository
    {
        Texture2D GetTexture2D(string key);
        void SetColor(string key, in Color color);
    }
}

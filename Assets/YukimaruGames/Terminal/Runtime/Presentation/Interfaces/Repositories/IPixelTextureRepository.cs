using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Repositories
{
    public interface IPixelTextureRepository
    {
        Texture2D GetTexture2D(string key);
        void SetColor(string key, in Color color);
    }
}

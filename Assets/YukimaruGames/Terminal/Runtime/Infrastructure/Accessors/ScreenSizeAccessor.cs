using UnityEngine;
using YukimaruGames.Terminal.Presentation.Contracts;

namespace YukimaruGames.Terminal.Infrastructure.Accessors
{
    public sealed class ScreenSizeAccessor : IScreenSizeProvider
    {
        public (int Width, int Height) Size => (Screen.width, Screen.height);
    }
}

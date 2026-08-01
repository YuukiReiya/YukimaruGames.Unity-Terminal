using UnityEngine;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Providers;

namespace YukimaruGames.Terminal.Infrastructure.Accessors
{
    /// <summary>
    /// <see cref="Screen"/>を介して画面サイズを提供する.
    /// </summary>
    public sealed class ScreenSizeAccessor : IScreenSizeProvider
    {
        /// <inheritdoc/>
        public (int Width, int Height) Size => (Screen.width, Screen.height);
    }
}

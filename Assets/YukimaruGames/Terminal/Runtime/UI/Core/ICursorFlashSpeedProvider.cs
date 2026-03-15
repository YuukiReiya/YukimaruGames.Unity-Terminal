using System;

namespace YukimaruGames.Terminal.UI.Core
{
    public interface ICursorFlashSpeedProvider
    {
        event Action<float> OnChangedFlashSpeed;
        float GetFlashSpeed();
    }
}

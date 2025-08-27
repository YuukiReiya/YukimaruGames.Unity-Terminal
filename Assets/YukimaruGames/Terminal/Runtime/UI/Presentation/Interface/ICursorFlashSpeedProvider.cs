using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ICursorFlashSpeedProvider
    {
        event Action<float> OnChangedFlashSpeed;
        float GetFlashSpeed();
    }
}

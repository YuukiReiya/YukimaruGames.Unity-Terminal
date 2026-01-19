using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ICursorFlashProvider
    {
        float FlashSpeed { get; }
        event Action<float> OnChangedFlashSpeed;
    }
}

using System;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface ICursorFlashSpeedAccessor :
        ICursorFlashSpeedMutator,
        ICursorFlashSpeedProvider
    {
        new float FlashSpeed { get; set; }
    }

    public interface ICursorFlashSpeedMutator
    {
        float FlashSpeed { set; }
    }
    
    public interface ICursorFlashSpeedProvider
    {
        event Action<float> OnChangedFlashSpeed;
        float FlashSpeed { get; }
    }
}

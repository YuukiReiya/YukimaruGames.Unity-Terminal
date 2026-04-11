using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Events
{
    public interface IKeyboardInputHandler
    {
        bool WasPressedThisFrame(Trigger action);
        bool WasReleasedThisFrame(Trigger action);
    }
}

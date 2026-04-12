using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Events
{
    public interface IKeyboardInputHandler
    {
        bool WasPressedThisFrame(TerminalAction action);
        bool WasReleasedThisFrame(TerminalAction action);
    }
}

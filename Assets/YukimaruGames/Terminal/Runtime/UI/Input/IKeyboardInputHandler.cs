using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Input
{
    public interface IKeyboardInputHandler
    {
        bool WasPressedThisFrame(TerminalAction action);
        bool WasReleasedThisFrame(TerminalAction action);
    }
}

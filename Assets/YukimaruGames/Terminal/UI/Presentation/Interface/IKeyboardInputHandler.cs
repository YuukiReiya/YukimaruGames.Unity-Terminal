using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime.Shared
{
    public interface IKeyboardInputHandler
    {
        bool WasPressedThisFrame(TerminalAction action);
        bool WasReleasedThisFrame(TerminalAction action);
    }
}

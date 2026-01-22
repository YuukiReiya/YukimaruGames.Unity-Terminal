using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Runtime.Shared
{
    public interface IKeyboardInputHandler
    {
        bool WasPressedThisFrame(TerminalAction action);
        bool WasReleasedThisFrame(TerminalAction action);
    }
}

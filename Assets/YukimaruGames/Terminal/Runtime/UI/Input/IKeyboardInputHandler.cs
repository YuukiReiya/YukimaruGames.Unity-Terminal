using YukimaruGames.Terminal.UI.Core;

namespace YukimaruGames.Terminal.UI.Input
{
    public interface IKeyboardInputHandler
    {
        bool WasPressedThisFrame(Trigger action);
        bool WasReleasedThisFrame(Trigger action);
    }
}

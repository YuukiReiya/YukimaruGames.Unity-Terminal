using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Models.Input
{
    public interface IInputKeyMap<out T>
    {
        T GetKey(TerminalAction action);
    }
}

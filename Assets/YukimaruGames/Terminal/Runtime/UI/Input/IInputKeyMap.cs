using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Input
{
    public interface IInputKeyMap<out T>
    {
        T GetKey(TerminalAction action);
    }
}

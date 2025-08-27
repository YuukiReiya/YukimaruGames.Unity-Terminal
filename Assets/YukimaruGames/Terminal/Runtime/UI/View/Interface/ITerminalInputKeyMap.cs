using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.View.Input
{
    public interface ITerminalInputKeyMap<out T>
    {
        T GetKey(TerminalAction action);
    }
}

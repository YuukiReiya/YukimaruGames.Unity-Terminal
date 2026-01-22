using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.UI.View.Input
{
    public interface ITerminalInputKeyMap<out T>
    {
        T GetKey(TerminalAction action);
    }
}

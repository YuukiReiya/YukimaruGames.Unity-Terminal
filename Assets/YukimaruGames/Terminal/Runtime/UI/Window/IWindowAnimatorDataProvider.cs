using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Window
{
    public interface IWindowAnimatorDataProvider
    {
        TerminalState State { get; }
        TerminalAnchor Anchor { get; }
        TerminalWindowStyle Style { get; }
        float Duration { get; }
        float Scale { get; }
    }
}

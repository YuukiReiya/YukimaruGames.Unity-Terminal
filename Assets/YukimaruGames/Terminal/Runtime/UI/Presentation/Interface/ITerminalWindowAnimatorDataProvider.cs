using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalWindowAnimatorDataProvider
    {
        TerminalState State { get; }
        TerminalAnchor Anchor { get; }
        TerminalWindowStyle Style { get; }
        float Duration { get; }
        float Scale { get; }
    }
}

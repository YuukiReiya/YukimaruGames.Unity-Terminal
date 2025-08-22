using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalWindowAnimatorDataConfigurator
    {
        TerminalState State { get; set; }
        TerminalAnchor Anchor { get; set; }
        TerminalWindowStyle Style { get; set; }
        float Duration { get; set; }
        float Scale { get; set; }
    }
}

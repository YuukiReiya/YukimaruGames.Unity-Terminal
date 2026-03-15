using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Window
{
    public interface IWindowAnimatorDataConfigurator
    {
        TerminalState State { get; set; }
        TerminalAnchor Anchor { get; set; }
        TerminalWindowStyle Style { get; set; }
        float Duration { get; set; }
        float Scale { get; set; }
    }
}

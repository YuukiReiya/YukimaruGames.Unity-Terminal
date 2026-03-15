using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Window
{
    public sealed class WindowAnimatorDataConfigurator : IWindowAnimatorDataConfigurator, IWindowAnimatorDataProvider
    {
        public TerminalState State { get; set; }
        public TerminalAnchor Anchor { get; set; }
        public TerminalWindowStyle Style { get; set; }
        public float Duration { get; set; }
        public float Scale { get; set; }
    }
}

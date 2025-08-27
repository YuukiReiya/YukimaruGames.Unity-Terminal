using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalWindowAnimatorData
    {
        public TerminalState State { get; }
        public TerminalAnchor Anchor { get; }
        public TerminalWindowStyle Style { get; }
        public float Duration { get; }
        public float Scale { get; }
        public float Elapsed { get; }

        public TerminalWindowAnimatorData(
            TerminalState state,
            TerminalAnchor anchor,
            TerminalWindowStyle style,
            float duration,
            float scale,
            float elapsed)
        {
            State = state;
            Anchor = anchor;
            Style = style;
            Duration = duration;
            Scale = scale;
            Elapsed = elapsed;
        }
    }
}

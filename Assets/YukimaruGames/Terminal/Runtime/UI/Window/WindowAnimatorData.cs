namespace YukimaruGames.Terminal.UI.Window
{
    public readonly struct WindowAnimatorData
    {
        public (int width, int height) Size { get; }
        public WindowState State { get; }
        public WindowAnchor Anchor { get; }
        public WindowStyle Style { get; }
        public float Duration { get; }
        public float Scale { get; }
        public float Elapsed { get; }

        public WindowAnimatorData(
            (int width,int height)size,
            WindowState state,
            WindowAnchor anchor,
            WindowStyle style,
            float duration,
            float scale,
            float elapsed)
        {
            Size = size;
            State = state;
            Anchor = anchor;
            Style = style;
            Duration = duration;
            Scale = scale;
            Elapsed = elapsed;
        }
    }
}

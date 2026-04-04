namespace YukimaruGames.Terminal.UI.Window
{
    public sealed class AnimationDataAccessor : IAnimationDataAccessor
    {
        public WindowState State { get; set; }
        public WindowAnchor Anchor { get; set; }
        public WindowStyle Style { get; set; }
        public float Duration { get; set; }
        public float Scale { get; set; }
    }
}

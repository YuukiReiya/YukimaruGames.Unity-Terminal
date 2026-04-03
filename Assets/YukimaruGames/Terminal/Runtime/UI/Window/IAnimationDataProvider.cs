namespace YukimaruGames.Terminal.UI.Window
{
    public interface IAnimationDataProvider
    {
        WindowState State { get; }
        WindowAnchor Anchor { get; }
        WindowStyle Style { get; }
        float Duration { get; }
        float Scale { get; }
    }
}

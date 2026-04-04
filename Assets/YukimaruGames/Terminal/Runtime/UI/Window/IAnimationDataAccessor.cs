namespace YukimaruGames.Terminal.UI.Window
{
    public interface IAnimationDataAccessor :
        IAnimationDataMutator,
        IAnimationDataProvider
    {
        new WindowState State { get; set; }
        new WindowAnchor Anchor { get; set; }
        new WindowStyle Style { get; set; }
        new float Duration { get; set; }
        new float Scale { get; set; }
    }

    public interface IAnimationDataMutator
    {
        WindowState State { set; }
        WindowAnchor Anchor { set; }
        WindowStyle Style { set; }
        float Duration { set; }
        float Scale { set; }
    }

    public interface IAnimationDataProvider
    {
        WindowState State { get; }
        WindowAnchor Anchor { get; }
        WindowStyle Style { get; }
        float Duration { get; }
        float Scale { get; }
    }
}
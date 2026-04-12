using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window
{
    public interface IWindowAnimationAccessor :
        IWindowAnimationMutator,
        IWindowAnimationProvider
    {
        new WindowState State { get; set; }
        new WindowAnchor Anchor { get; set; }
        new WindowStyle Style { get; set; }
        new float Duration { get; set; }
        new float Scale { get; set; }
    }

    public interface IWindowAnimationMutator
    {
        WindowState State { set; }
        WindowAnchor Anchor { set; }
        WindowStyle Style { set; }
        float Duration { set; }
        float Scale { set; }
    }

    public interface IWindowAnimationProvider
    {
        WindowState State { get; }
        WindowAnchor Anchor { get; }
        WindowStyle Style { get; }
        float Duration { get; }
        float Scale { get; }
    }
}
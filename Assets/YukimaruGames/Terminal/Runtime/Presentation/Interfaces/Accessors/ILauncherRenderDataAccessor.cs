using YukimaruGames.Terminal.Presentation.Models.Launcher;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface ILauncherRenderDataAccessor :
        ILauncherRenderDataMutator,
        ILauncherRenderDataProvider
    {
        new LauncherRenderData RenderData { get; set; }
    }

    public interface ILauncherRenderDataMutator
    {
        LauncherRenderData RenderData { set; }
    }

    public interface ILauncherRenderDataProvider
    {
        LauncherRenderData RenderData { get; }
    }
}
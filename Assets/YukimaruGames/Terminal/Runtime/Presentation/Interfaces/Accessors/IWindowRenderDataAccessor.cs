using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface IWindowRenderDataAccessor :
        IWindowRenderDataMutator,
        IWindowRenderDataProvider
    {
        new WindowRenderData RenderData { get; set; }
    }

    public interface IWindowRenderDataMutator
    {
        WindowRenderData RenderData { set; }
    }

    public interface IWindowRenderDataProvider
    {
        WindowRenderData RenderData { get; }
    }
}

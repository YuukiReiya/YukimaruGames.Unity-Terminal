using YukimaruGames.Terminal.Presentation.Models.Submit;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface ISubmitRenderDataAccessor :
        ISubmitRenderDataMutator,
        ISubmitRenderDataProvider
    {
        new SubmitRenderData RenderData { get; set; }
    }

    public interface ISubmitRenderDataMutator
    {
        SubmitRenderData RenderData { set; }
    }

    public interface ISubmitRenderDataProvider
    {
        SubmitRenderData RenderData { get; }
    }
}
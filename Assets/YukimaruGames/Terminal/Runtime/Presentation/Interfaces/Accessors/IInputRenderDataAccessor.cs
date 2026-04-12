using YukimaruGames.Terminal.Presentation.Models.Input;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface IInputRenderDataAccessor :
        IInputRenderDataMutator,
        IInputRenderDataProvider
    {
        new InputRenderData RenderData { get; set; }
    }
    
    public interface IInputRenderDataMutator
    {
        InputRenderData RenderData { set; }
    }

    public interface IInputRenderDataProvider
    {
        InputRenderData RenderData { get; }
    }
}

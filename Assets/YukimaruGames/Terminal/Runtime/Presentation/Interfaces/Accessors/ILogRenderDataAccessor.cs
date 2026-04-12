using YukimaruGames.Terminal.Presentation.Models.Log;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface ILogRenderDataAccessor :
        ILogRenderDataMutator,
        ILogRenderDataProvider
    {
        new LogRenderData RenderData { get; set; }
    }

    public interface ILogRenderDataMutator
    {
        LogRenderData RenderData { set; }
    }
    
    public interface ILogRenderDataProvider
    {
        /// <summary>
        /// ログ表示のための描画データの取得.
        /// </summary>
        LogRenderData RenderData { get; }
    }
}

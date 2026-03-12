using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.Log
{
    public interface ILogRenderDataProvider
    {
        /// <summary>
        /// ログ表示のための描画データの取得.
        /// </summary>
        LogRenderData GetRenderData();
    }
}

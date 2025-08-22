using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalLogRenderDataProvider
    {
        /// <summary>
        /// ログ表示のための描画データの取得.
        /// </summary>
        TerminalLogRenderData GetRenderData();
    }
}

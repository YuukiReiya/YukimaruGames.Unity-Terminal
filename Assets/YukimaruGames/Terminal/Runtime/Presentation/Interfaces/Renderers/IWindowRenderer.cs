using System;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    /// <summary>
    /// ウィンドウの描画クラス.
    /// </summary>
    public interface IWindowRenderer
    {
        /// <summary>
        /// 描画.
        /// </summary>
        /// <param name="viewModel">描画に利用するパラメータ</param>
        /// <param name="func">ウィンドウ内部のコンテンツを描画するコールバック</param>
        void Render(WindowRenderData viewModel, Action<int> func);
    }
}

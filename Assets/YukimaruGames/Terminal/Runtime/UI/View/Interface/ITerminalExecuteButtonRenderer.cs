using System;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    /// <summary>
    /// 実行ボタンの描画クラス
    /// </summary>
    public interface ITerminalExecuteButtonRenderer
    {
        /// <summary>
        /// 表示テキスト
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// 描画
        /// </summary>
        /// <param name="renderData">描画に利用するパラメータ</param>
        void Render(TerminalExecuteButtonRenderData renderData);

        /// <summary>
        /// ボタンのクリック処理.
        /// </summary>
        event Action OnClickButton;
    }
}

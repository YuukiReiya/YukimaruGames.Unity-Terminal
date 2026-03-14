using System;

namespace YukimaruGames.Terminal.UI.Submit
{
    /// <summary>
    /// 実行ボタンの描画クラス
    /// </summary>
    public interface ISubmitRenderer
    {
        /// <summary>
        /// 表示テキスト
        /// </summary>
        string DisplayText { get; }

        /// <summary>
        /// 描画
        /// </summary>
        /// <param name="renderData">描画に利用するパラメータ</param>
        void Render(SubmitRenderData renderData);

        /// <summary>
        /// ボタンのクリック処理.
        /// </summary>
        event Action OnClickButton;
    }
}

using YukimaruGames.Terminal.Presentation.Contracts;

namespace YukimaruGames.Terminal.Adapters.GUI
{
    /// <summary>
    /// <see cref="ICursorView"/>の実装。<see cref="Presentation.Presenters.CursorPresenter"/>から
    /// 通知される表示状態を保持し、IMGUI描画側（<see cref="Renderers.InputRenderer"/>）から参照される.
    /// </summary>
    public sealed class CursorView : ICursorView
    {
        /// <summary>現在カーソルを表示すべきかどうか.</summary>
        public bool IsVisible { get; private set; } = true;

        void ICursorView.SetVisible(bool visible)
        {
            IsVisible = visible;
        }
    }
}

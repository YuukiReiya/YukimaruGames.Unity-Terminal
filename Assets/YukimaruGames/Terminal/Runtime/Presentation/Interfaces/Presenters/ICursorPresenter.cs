namespace YukimaruGames.Terminal.Presentation.Interfaces.Presenters
{
    public interface ICursorPresenter
    {
        /// <summary>現在カーソルを表示すべきかどうか.</summary>
        bool IsVisible { get; }
    }
}

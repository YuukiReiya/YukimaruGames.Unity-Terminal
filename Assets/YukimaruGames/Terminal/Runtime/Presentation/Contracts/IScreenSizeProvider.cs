namespace YukimaruGames.Terminal.Presentation.Contracts
{
    /// <summary>
    /// 画面サイズの提供者.
    /// <para>
    /// Presentation層はUnityEngine.Screenに直接依存しないため、この抽象を介して画面サイズを取得する。
    /// </para>
    /// </summary>
    public interface IScreenSizeProvider
    {
        /// <summary>現在の画面サイズ.</summary>
        (int Width, int Height) Size { get; }
    }
}

namespace YukimaruGames.Terminal.Presentation.Models.Submit
{
    /// <summary>
    /// 実行ボタンの描画データ
    /// </summary>
    public readonly struct SubmitRenderData
    {
        public bool IsVisible { get; }

        public SubmitRenderData(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}

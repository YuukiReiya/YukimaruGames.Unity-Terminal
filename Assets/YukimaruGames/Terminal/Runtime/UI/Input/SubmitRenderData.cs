namespace YukimaruGames.Terminal.UI.Input
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

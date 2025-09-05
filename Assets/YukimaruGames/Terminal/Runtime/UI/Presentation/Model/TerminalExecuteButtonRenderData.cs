namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    /// <summary>
    /// 実行ボタンの描画データ
    /// </summary>
    public readonly struct TerminalExecuteButtonRenderData
    {
        public bool IsVisible { get; }

        public TerminalExecuteButtonRenderData(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}

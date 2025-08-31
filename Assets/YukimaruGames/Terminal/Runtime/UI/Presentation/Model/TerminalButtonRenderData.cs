namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalButtonRenderData
    {
        public bool IsVisible { get; }

        public TerminalButtonRenderData(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}

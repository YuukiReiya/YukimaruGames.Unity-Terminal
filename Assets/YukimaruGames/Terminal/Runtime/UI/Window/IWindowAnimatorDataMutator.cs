namespace YukimaruGames.Terminal.UI.Window
{
    public interface IWindowAnimatorDataMutator
    {
        WindowState State { get; set; }
        WindowAnchor Anchor { get; set; }
        WindowStyle Style { get; set; }
        float Duration { get; set; }
        float Scale { get; set; }
    }
}

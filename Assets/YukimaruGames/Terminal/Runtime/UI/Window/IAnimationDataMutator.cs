namespace YukimaruGames.Terminal.UI.Window
{
    public interface IAnimationDataMutator
    {
        WindowState State { set; }
        WindowAnchor Anchor { set; }
        WindowStyle Style { set; }
        float Duration { set; }
        float Scale { set; }
    }
}

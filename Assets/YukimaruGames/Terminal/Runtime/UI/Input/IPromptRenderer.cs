namespace YukimaruGames.Terminal.UI.Input
{
    public interface IPromptRenderer
    {
        string Prompt { set; }
        void Render();
    }
}

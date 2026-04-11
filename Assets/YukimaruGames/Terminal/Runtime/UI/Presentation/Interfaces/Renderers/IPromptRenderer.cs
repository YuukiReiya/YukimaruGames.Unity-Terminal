namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IPromptRenderer
    {
        string Prompt { set; }
        void Render();
    }
}

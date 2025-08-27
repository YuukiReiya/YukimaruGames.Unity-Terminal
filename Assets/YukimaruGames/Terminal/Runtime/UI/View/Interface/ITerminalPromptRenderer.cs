namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalPromptRenderer
    {
        string Prompt { set; }
        void Render();
    }
}

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IPromptRenderer
    {
        string Prompt { set; }

        /// <summary>
        /// コマンド実行中にローディング表現を行うかどうか.
        /// </summary>
        bool ShowLoadingIndicator { set; }

        void Render();
    }
}

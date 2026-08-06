namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IPromptRenderer
    {
        /// <summary>
        /// コマンド実行中にローディング表現を行うかどうか.
        /// </summary>
        bool ShowLoadingIndicator { set; }

        /// <summary>
        /// ローディング表現として順番に表示するフレーム文字列群.
        /// </summary>
        string[] LoadingIndicatorFrames { set; }

        void Render();
    }
}

namespace YukimaruGames.Terminal.Runtime
{
    public interface IInstaller
    {
        /// <summary>
        /// アプリケーションを構築し、Scopeを返す.
        /// </summary>
        TerminalRuntimeScope Install();

        /// <summary>
        /// アプリケーションを破棄する.
        /// </summary>
        void Uninstall(TerminalRuntimeScope scope);
    }
}

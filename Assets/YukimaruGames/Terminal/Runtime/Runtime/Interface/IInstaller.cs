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
        /// <summary>
        /// アプリケーションの設定を再解決（再適用）する.
        /// </summary>
        void Resolve(TerminalRuntimeScope scope);
    }
}

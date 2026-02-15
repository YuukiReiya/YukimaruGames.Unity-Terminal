using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.Runtime
{
    public interface ITerminalInstaller
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

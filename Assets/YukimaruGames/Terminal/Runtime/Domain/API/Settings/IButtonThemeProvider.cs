namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <summary>
    /// ボタンの配色設定を提供する機能を表します。
    /// </summary>
    public interface IButtonThemeProvider
    {
        /// <summary>ボタンの配色設定を取得します。</summary>
        ITerminalButtonTheme ButtonTheme { get; }
    }
}

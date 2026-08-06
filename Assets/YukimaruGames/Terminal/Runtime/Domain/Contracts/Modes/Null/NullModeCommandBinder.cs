using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes.Null
{
    /// <summary>
    /// モード専用コマンドの束縛機能が配線されていない場合の既定値.
    /// </summary>
    /// <remarks>
    /// 常に <see cref="NullCommandRegistry"/> を返す(属性発見も動的Addも機能しない、
    /// 完全に不活性なレジストリ)。<c>IModeContext.Commands</c> を絶対に <c>null</c> にしない
    /// ための最終フォールバック。実際に機能させるには <c>ModeCommandBinder</c>(Infrastructure)
    /// の配線が必要.
    /// </remarks>
    public sealed class NullModeCommandBinder : IModeCommandBinder
    {
        /// <summary>
        /// 唯一のインスタンス.
        /// </summary>
        public static readonly NullModeCommandBinder Instance = new();

        private NullModeCommandBinder()
        {
        }

        ICommandRegistry IModeCommandBinder.BindFor(ITerminalMode mode) => NullCommandRegistry.Instance;
    }
}

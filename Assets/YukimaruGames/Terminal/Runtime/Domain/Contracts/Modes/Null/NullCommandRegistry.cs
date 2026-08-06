using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes.Null
{
    /// <summary>
    /// コマンド登録機能を持たない Null Object.
    /// </summary>
    /// <remarks>
    /// モード専用コマンドのレジストリ生成手段が配線されていない場合の既定値として使用する
    /// (<c>IModeContext.Commands</c> を絶対に<c>null</c>にしないため).
    /// </remarks>
    public sealed class NullCommandRegistry : ICommandRegistry
    {
        /// <summary>
        /// 唯一のインスタンス.
        /// </summary>
        public static readonly NullCommandRegistry Instance = new();

        private NullCommandRegistry()
        {
        }

        bool ICommandRegistry.Add(string command, CommandHandler handle) => false;

        bool ICommandRegistry.Remove(string command) => false;

        bool ICommandRegistry.TryGet(string command, out CommandHandler handler)
        {
            handler = default;
            return false;
        }
    }
}

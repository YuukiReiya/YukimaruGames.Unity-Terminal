using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services
{
    /// <summary>
    /// コマンドの実行インターフェイス.
    /// </summary>
    public interface ICommandInvoker
    {
        /// <summary>
        /// コマンドの実行.
        /// </summary>
        /// <param name="handler">実行するコマンドハンドル</param>
        /// <param name="arguments">引数</param>
        void Execute(CommandHandler handler, CommandArgument[] arguments);
    }
}

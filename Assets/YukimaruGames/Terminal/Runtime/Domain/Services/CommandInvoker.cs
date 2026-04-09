using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Services
{
    /// <summary>
    /// コマンドの実行クラス.
    /// </summary>
    public sealed class CommandInvoker : ICommandInvoker
    {
        /// <inheritdoc/>
        public void Execute(CommandHandler handler, CommandArgument[] arguments)
        {
            handler.Proc(arguments);
        }
    }
}

using YukimaruGames.Terminal.Domain.API.Commands;

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

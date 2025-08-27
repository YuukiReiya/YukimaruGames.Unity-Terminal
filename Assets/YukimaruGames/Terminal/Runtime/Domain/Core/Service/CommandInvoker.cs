using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Domain.Service
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

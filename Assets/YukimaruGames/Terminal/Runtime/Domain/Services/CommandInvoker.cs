using System;
using System.Threading;
using System.Threading.Tasks;
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
        public void Execute(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments)
        {
            handler.Proc?.Invoke(arguments);
        }

        /// <inheritdoc/>
        public ValueTask ExecuteAsync(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments, CancellationToken cancellationToken)
        {
            return handler.AsyncProc!(arguments, cancellationToken);
        }
    }
}

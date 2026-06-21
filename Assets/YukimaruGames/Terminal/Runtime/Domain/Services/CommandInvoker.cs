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
            if (handler.IsAsync)
            {
                throw new ArgumentException(
                    $"The command '{handler.Meta.Command}' is registered as asynchronous but was invoked synchronously.", 
                    nameof(handler));
            }
            
            handler.Proc?.Invoke(arguments);
        }

        /// <inheritdoc/>
        public ValueTask ExecuteAsync(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments, CancellationToken cancellationToken)
        {
            if (!handler.IsAsync)
            {
                throw new ArgumentException(
                    $"The command '{handler.Meta.Command}' is registered as synchronous but was invoked asynchronously.",
                    nameof(handler));
            }

            return handler.AsyncProc!(arguments, cancellationToken);
        }
    }
}

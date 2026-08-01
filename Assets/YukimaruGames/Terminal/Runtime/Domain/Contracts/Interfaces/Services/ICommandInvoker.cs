using System;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services
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
        void Execute(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments);
        
        /// <summary>
        /// 非同期コマンドの実行.
        /// </summary>
        /// <param name="handler">実行するコマンドハンドル</param>
        /// <param name="arguments">引数</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        ValueTask ExecuteAsync(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments, CancellationToken cancellationToken);
    }
}

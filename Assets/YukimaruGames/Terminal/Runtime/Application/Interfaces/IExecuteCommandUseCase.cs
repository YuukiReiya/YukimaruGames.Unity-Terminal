using System;
using System.Threading;
using System.Threading.Tasks;

namespace YukimaruGames.Terminal.Application.Interfaces
{
    /// <summary>
    /// コマンド実行ユースケースのインターフェイス.
    /// </summary>
    public interface IExecuteCommandUseCase
    {
        /// <summary>
        /// コマンドを同期実行する.
        /// </summary>
        /// <param name="str">入力文字列</param>
        void Execute(ReadOnlyMemory<char> str);
        
        /// <summary>
        /// コマンドを同期実行する.
        /// </summary>
        /// <param name="str">入力文字列</param>
        void Execute(string str);
        
        /// <summary>
        /// コマンドを非同期で実行する.
        /// </summary>
        /// <param name="str">入力文字列</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        ValueTask ExecuteAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken);

        /// <summary>
        /// コマンドを非同期で実行する.
        /// </summary>
        /// <param name="str">入力文字列</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        ValueTask ExecuteAsync(string str, CancellationToken cancellationToken);
    }
}
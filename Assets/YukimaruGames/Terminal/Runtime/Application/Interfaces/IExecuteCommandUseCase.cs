using System;
using System.Threading.Tasks;

namespace YukimaruGames.Terminal.Application.Interfaces
{
    /// <summary>
    /// コマンド実行ユースケースのインターフェイス.
    /// </summary>
    public interface IExecuteCommandUseCase
    {
        /// <summary>
        /// コマンドを非同期で実行する.
        /// </summary>
        /// <param name="str">入力文字列</param>
        ValueTask ExecuteAsync(ReadOnlyMemory<char> str);

        /// <summary>
        /// コマンドを非同期で実行する.
        /// </summary>
        /// <param name="str">入力文字列</param>
        ValueTask ExecuteAsync(string str);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services
{
    /// <summary>
    /// コマンド引数のパーサーインターフェイス.
    /// </summary>
    public interface ICommandParser
    {
        [Flags]
        public enum ParseStatusCode : byte
        {
            /// <summary>
            /// 成功
            /// </summary>
            Ok = 1,

            /// <summary>
            /// 構文エラー.
            /// </summary>
            SyntaxError = 2,

            /// <summary>
            /// 不正な形式の入力エラー.
            /// </summary>
            MalformedInput = 4,

            /// <summary>
            /// 内部エラー.
            /// </summary>
            InternalError = 255,
        }

        /// <summary>
        /// 文字列からコマンド引数型へパースを試行する.
        /// </summary>
        /// <param name="str">解析文字列</param>
        /// <param name="tuple">ValueTuple 
        /// <p>* Command:コマンド名</p>
        /// <p>* Arguments:引数</p>
        /// </param>
        /// <returns>パース結果のステータスコード</returns>
        ParseStatusCode Parse(string str, out (string Command, CommandArgument[] Arguments) tuple);

        /// <summary>
        /// 文字列メモリからコマンド引数型へパースを試行する.
        /// </summary>
        /// <param name="str">解析文字列</param>
        /// <param name="tuple">ValueTuple 
        /// <p>* Command:コマンド名</p>
        /// <p>* Arguments:引数</p>
        /// </param>
        /// <returns>パース結果のステータスコード</returns>
        ParseStatusCode Parse(ReadOnlyMemory<char> str, out (string Command, CommandArgument[] Arguments) tuple);

        /// <summary>
        /// 文字列メモリからコマンド引数型へ非同期でパースを試行する.
        /// </summary>
        /// <param name="str">解析文字列</param>
        /// <param name="cancellationToken">キャンセルトークン</param>
        /// <returns>パース結果.</returns>
        [Obsolete("パフォーマンス向上のため、原則同期メソッドである 'Parse' を使用してください。"), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        ValueTask<(ParseStatusCode Status, string Command, CommandArgument[] Arguments)> ParseAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken);
    }
}
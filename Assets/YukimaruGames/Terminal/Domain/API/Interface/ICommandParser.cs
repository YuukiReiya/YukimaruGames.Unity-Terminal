using System;
using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Domain.Interface
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
        ParseStatusCode Parse(string str, out (string Command, CommandArgument[]Arguments) tuple);
    }
}

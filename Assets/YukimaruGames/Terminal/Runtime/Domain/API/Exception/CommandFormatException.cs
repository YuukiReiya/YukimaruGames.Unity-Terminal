using System;
using System.Runtime.Serialization;

namespace YukimaruGames.Terminal.Domain.Exception
{
    /// <summary>
    /// コマンド引数の型変換失敗時に送出するカスタム例外.
    /// </summary>
    [Serializable]
    public sealed class CommandFormatException : FormatException
    {
        /// <summary>
        /// 引数インデックス.
        /// </summary>
        /// <remarks>
        /// <p><see cref="CommandDelegate"/></p>
        /// 配列で受け取る引数の対象インデックス.
        /// </remarks>
        public int ArgumentIndex { get; }
        
        /// <summary>
        /// 失敗した値.
        /// </summary>
        public string FailedValue { get; }
        
        /// <summary>
        /// フォーマット(変換)に失敗した目的の型.
        /// </summary>
        public Type TargetType { get; }

        public CommandFormatException(int argumentIndex, string failedValue, Type targetType, System.Exception innerException)
            : base(BuildMessage(argumentIndex, failedValue, targetType), innerException)
        {
            ArgumentIndex = argumentIndex;
            FailedValue = failedValue;
            TargetType = targetType;
        }

        private CommandFormatException(SerializationInfo info, StreamingContext context) 
            : base(info,context)
        {
        }

        /// <summary>
        /// 例外送出メッセージ.
        /// </summary>
        /// <param name="index">引数インデックス</param>
        /// <param name="value">失敗した変数</param>
        /// <param name="type">失敗した変換先の型</param>
        /// <returns></returns>
        private static string BuildMessage(int index, string value, Type type) =>
            $"Argument at index {index} (value: '{value}') could not be converted to type '{type.Name}'.";
    }
}

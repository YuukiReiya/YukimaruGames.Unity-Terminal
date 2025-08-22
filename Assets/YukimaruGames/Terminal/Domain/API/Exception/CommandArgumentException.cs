using System;
using System.Runtime.Serialization;

namespace YukimaruGames.Terminal.Domain.Exception
{
    /// <summary>
    /// コマンド引数の引数の数が違う場合に送出するカスタム例外.
    /// </summary>
    [Serializable]
    public sealed class CommandArgumentException : ArgumentException
    {
        /// <summary>
        /// 実際に渡された引数の数.
        /// </summary>
        public int Actual { get; }
        
        /// <summary>
        /// 期待される引数の最小数.
        /// </summary>
        public int ExpectedMin { get; }
        
        /// <summary>
        /// 期待される引数の最大数.
        /// </summary>
        public int ExpectedMax { get; }

        public CommandArgumentException(int actual, int expectedMin, int expectedMax, System.Exception innerException)
            : base(BuildMessage(actual, expectedMin, expectedMax), innerException)
        {
            Actual = actual;
            ExpectedMin = expectedMin;
            ExpectedMax = expectedMax;
        }

        private CommandArgumentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// 例外送出メッセージ.
        /// </summary>
        /// <param name="actual">引数</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static string BuildMessage(int actual, int min, int max)
        {
            string expected;

            // 引数が足りない or 不正な引数の登録.
            if (actual < min)
            {
                expected = $"{min}";
            }
            // 引数が多い or 不正な引数登録.
            else if (-1 < max && max < actual)
            {
                expected = $"{min} or more";
            }
            else
            {
                expected = $"between {min} and {max}";
            }

            var pluralFix = (min == 1 && max == 1) ? "" : "s";
            return
                $"Invalid argument count. Expected {expected} argument{pluralFix}, but received {actual}.";
        }
    }
}

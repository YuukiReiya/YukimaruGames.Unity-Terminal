using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Services
{
    /// <summary>
    /// コマンド引数のパーサー.
    /// </summary>
    public sealed class CommandParser : ICommandParser
    {
        private static readonly char[] Delimiters =
        {
            // 空白(半角)
            ' ',
            // 空白(全角)
            '　',
            // タブ
            '\t'
        };

        /// <summary>
        /// 文字列からコマンド引数型へパース.
        /// </summary>
        /// <param name="str">解析文字列</param>
        /// <param name="tuple">
        /// <p>Command:コマンド名</p>
        /// <p>Arguments:引数</p>
        /// </param>
        /// <returns>
        /// Parseの成功可否.
        /// </returns>
        public ICommandParser.ParseStatusCode Parse(string str, out (string Command, CommandArgument[] Arguments) tuple)
        {
            if (string.IsNullOrEmpty(str))
            {
                tuple = default;
                return ICommandParser.ParseStatusCode.MalformedInput;
            }

            return ParseCore(str.AsMemory(), out tuple);
        }

        /// <summary>
        /// 文字列メモリからコマンド引数型へパース.
        /// </summary>
        /// <param name="str">解析文字列</param>
        /// <param name="tuple">
        /// <p>Command:コマンド名</p>
        /// <p>Arguments:引数</p>
        /// </param>
        /// <returns>Parseの成功可否.</returns>
        public ICommandParser.ParseStatusCode Parse(ReadOnlyMemory<char> str, out (string Command, CommandArgument[] Arguments) tuple)
        {
            if (str.IsEmpty)
            {
                tuple = default;
                return ICommandParser.ParseStatusCode.MalformedInput;
            }

            return ParseCore(str, out tuple);
        }

        /// <summary>
        /// 非同期で文字列メモリからコマンド引数型へパース.
        /// </summary>
        /// <param name="str">解析文字列</param>
        /// <returns>パース結果.</returns>
        public ValueTask<(ICommandParser.ParseStatusCode Status, string Command, CommandArgument[] Arguments)> ParseAsync(ReadOnlyMemory<char> str)
        {
            var status = Parse(str, out var tuple);
            return new ValueTask<(ICommandParser.ParseStatusCode Status, string Command, CommandArgument[] Arguments)>((status, tuple.Command, tuple.Arguments));
        }

        /// <summary>
        /// 文字列メモリからコマンド引数型へパース.
        /// </summary>
        /// <param name="source">解析文字列</param>
        /// <param name="tuple">
        /// <p>Command:コマンド名</p>
        /// <p>Arguments:引数</p>
        /// </param>
        /// <returns>Parseの成功可否.</returns>
        private static ICommandParser.ParseStatusCode ParseCore(ReadOnlyMemory<char> source, out (string Command, CommandArgument[] Arguments) tuple)
        {
            var span = source.Span;
            var firstDelimiter = span.IndexOfAny(Delimiters);
            if (firstDelimiter is -1 or 0)
            {
                tuple = (source.ToString(), Array.Empty<CommandArgument>());
                return ICommandParser.ParseStatusCode.Ok;
            }

            var command = source.Slice(0, firstDelimiter).ToString();
            var remainder = source.Slice(firstDelimiter + 1);
            var result = TryExtractArguments(remainder, out var arguments);
            if (result is not ICommandParser.ParseStatusCode.Ok)
            {
                tuple = (command, null);
                return result;
            }

            tuple = (command, arguments);
            return ICommandParser.ParseStatusCode.Ok;
        }

        /// <summary>
        /// 文字列の中から引数を取得.
        /// </summary>
        /// <param name="text">文字列</param>
        /// <param name="args">引数</param>
        /// <returns>解析された引数</returns>
        /// <remarks>
        /// ""(ダブルクォート),''(シングルクォート)で括られた空白文字は考慮する.
        /// </remarks>
        private static ICommandParser.ParseStatusCode TryExtractArguments(ReadOnlyMemory<char> text, out CommandArgument[] args)
        {
            var span = text.Span;
            if (span.IsEmpty)
            {
                args = Array.Empty<CommandArgument>();
                return ICommandParser.ParseStatusCode.Ok;
            }

            var results = new List<CommandArgument>();
            var pos = 0;
            var len = span.Length;

            while (pos < len)
            {
                while (pos < len && IsDelimiter(span[pos]))
                {
                    pos++;
                }

                if (pos >= len)
                {
                    break;
                }

                var argStart = pos;
                var argEnd = pos;
                var inQuote = false;
                var quote = '\0';

                while (pos < len)
                {
                    var current = span[pos];

                    if (!inQuote)
                    {
                        if (IsDelimiter(current))
                        {
                            break;
                        }

                        if (current is '\'' or '"')
                        {
                            inQuote = true;
                            quote = current;
                            argStart = pos + 1;
                            argEnd = argStart;
                            pos++;
                            continue;
                        }

                        argEnd = pos + 1;
                        pos++;
                        continue;
                    }

                    if (inQuote && current == quote)
                    {
                        inQuote = false;
                        argEnd = pos;
                        pos++;
                        continue;
                    }

                    argEnd = pos + 1;
                    pos++;
                }

                if (inQuote)
                {
                    args = Array.Empty<CommandArgument>();
                    return ICommandParser.ParseStatusCode.SyntaxError;
                }

                var argLength = argEnd - argStart;
                if (argLength < 0)
                {
                    argLength = 0;
                }

                results.Add(new CommandArgument(text.Slice(argStart, argLength)));
            }

            args = results.Count == 0 ? Array.Empty<CommandArgument>() : results.ToArray();
            return ICommandParser.ParseStatusCode.Ok;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDelimiter(char value) => value is ' ' or '　' or '\t';
    }
}

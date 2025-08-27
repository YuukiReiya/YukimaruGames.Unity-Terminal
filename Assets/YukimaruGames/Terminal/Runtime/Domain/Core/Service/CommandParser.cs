using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Domain.Service
{
    /// <summary>
    /// コマンド引数のパーサー.
    /// </summary>
    public sealed class CommandParser : ICommandParser
    {
        private char[] _delimiters;
        private char[] Delimiters => _delimiters ??= new[]
        {
            // 空白(半角)
            ' ',
            // 空白(全角)
            '　',
            //  タブ
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
        public ICommandParser.ParseStatusCode Parse(string str, out (string Command, CommandArgument[]Arguments) tuple)
        {
            if (string.IsNullOrEmpty(str))
            {
                tuple = default;
                return ICommandParser.ParseStatusCode.MalformedInput;
            }

            var builder = new StringBuilder(str);
            var index = str.IndexOfAny(Delimiters);
            if (index is -1 or 0)
            {
                tuple = (str, Array.Empty<CommandArgument>());
                return ICommandParser.ParseStatusCode.Ok;
            }

            var command = str[..(index)];
            var text = builder.Remove(0, index + 1).ToString();

            var result = TryExtractArguments(text, out var args);
            if (result is ICommandParser.ParseStatusCode.Ok)
            {
                var commandArguments = Convert(args);
                tuple = (command, commandArguments);
                return ICommandParser.ParseStatusCode.Ok;
            }

            tuple = (str, null);
            return result;
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
        private ICommandParser.ParseStatusCode TryExtractArguments(string text,out string[]args)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                args = default;
                return ICommandParser.ParseStatusCode.Ok;
            }
            
            const char singleQuote = '\'';
            const char doubleQuote = '\"';
            var quotes =
                new (char Head, char Tail)[] { (singleQuote, singleQuote), (doubleQuote, doubleQuote) };

            var sb = new StringBuilder();
            (char Head, char Tail)? tmp = null;
            
            var pos = 0;
            var len = text.Length;
            var queue = new Queue<string>();
            

            while (pos < len)
            {
                var s = text[pos];

                if (tmp.HasValue)
                {
                    if (s.Equals(tmp.Value.Tail))
                    {
                        tmp = null;
                    }
                }
                else
                {
                    // 区切り文字.
                    if (Delimiters.Contains(s) && 0 < sb.Length)
                    {
                        queue.Enqueue(sb.ToString());
                        sb.Clear();
                        pos++;
                        continue;
                    }
                    else
                    {
                        // 文字列リテラル.
                        foreach (var quote in quotes)
                        {
                            if (s.Equals(quote.Head))
                            {
                                tmp = quote;
                                break;
                            }
                        }
                    }
                }

                sb.Append(s);
                pos++;
            }

            if (0 < sb.Length)
            {
                queue.Enqueue(sb.ToString());
            }

            args = new string[queue.Count];

            for (var i = 0; i < args.Length; i++) args[i] = queue.Dequeue();

            if (tmp.HasValue)
            {
                return ICommandParser.ParseStatusCode.SyntaxError;
            }
            
            return ICommandParser.ParseStatusCode.Ok;
        }

        private CommandArgument[] Convert(string[] arguments)
        {
            if (arguments is null)
            {
                return Array.Empty<CommandArgument>();
            }

            var args = new CommandArgument[arguments.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                var arg = arguments[i];
                args[i] = new CommandArgument(arg);
            }

            return args;
        }
    }
}

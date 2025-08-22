using System;
using System.Collections.Generic;
using System.Linq;
using YukimaruGames.Terminal.Domain.Interface;

namespace YukimaruGames.Terminal.Domain.Service
{
    /// <summary>
    /// コマンドの自動補完クラス.
    /// </summary>
    public sealed class CommandAutocomplete : ICommandAutocomplete
    {
        private readonly HashSet<string> _knownWords = new();
        private readonly List<string> _buffer = new();

        /// <inheritdoc/>
        public IEnumerable<string> KnownWords => _knownWords ?? Enumerable.Empty<string>();

        /// <inheritdoc/>
        public bool Register(string word) => _knownWords.Add(word);

        /// <inheritdoc/>
        public string[] Complete(string text)
        {
            if (string.IsNullOrEmpty(text)) return default;
            
            // 部分文字列.
            var partialWord = GetLastWord(text);
            if (string.IsNullOrEmpty(partialWord))
            {
                return default;
            }

            _buffer.Clear();

            foreach (var knownWord in _knownWords)
            {
                if (knownWord.StartsWith(partialWord,StringComparison.OrdinalIgnoreCase))
                {
                    _buffer.Add(knownWord);
                }
            }

            return _buffer.ToArray();
        }

        /// <summary>
        /// 最後の単語を取得.
        /// </summary>
        /// <param name="text">文字列</param>
        /// <remarks>
        /// 文字列の中から最後に登場した空白文字(半角スペース、全角スペース、タブ文字列)の位置以降の文字が最後に出現した単語.
        /// </remarks>
        private string GetLastWord(string text)
        {
            var lastIndex = text.LastIndexOfAny(new[] { ' ', '　', '\t', });
            var result = text[(lastIndex + 1)..];
            return result;
        }
    }
}

using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes.Null
{
    /// <summary>
    /// 自動補完機能を無効化するための Null Object.
    /// </summary>
    public sealed class NullCommandAutocomplete : ICommandAutocomplete
    {
        /// <summary>
        /// 唯一のインスタンス.
        /// </summary>
        public static readonly NullCommandAutocomplete Instance = new();

        private NullCommandAutocomplete()
        {
        }

        IEnumerable<string> ICommandAutocomplete.KnownWords => System.Array.Empty<string>();

        bool ICommandAutocomplete.Register(string word) => false;

        bool ICommandAutocomplete.Unregister(string word) => false;

        string[] ICommandAutocomplete.Complete(string text) => System.Array.Empty<string>();
    }
}

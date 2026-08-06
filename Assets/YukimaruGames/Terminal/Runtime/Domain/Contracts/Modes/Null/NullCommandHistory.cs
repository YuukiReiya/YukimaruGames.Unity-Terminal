using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes.Null
{
    /// <summary>
    /// 履歴機能を無効化するための Null Object.
    /// </summary>
    public sealed class NullCommandHistory : ICommandHistory
    {
        /// <summary>
        /// 唯一のインスタンス.
        /// </summary>
        public static readonly NullCommandHistory Instance = new();

        private NullCommandHistory()
        {
        }

        IReadOnlyCollection<string> ICommandHistory.Histories => System.Array.Empty<string>();

        void ICommandHistory.Clear()
        {
        }

        bool ICommandHistory.Add(string str) => false;

        string ICommandHistory.Next() => string.Empty;

        string ICommandHistory.Previous() => string.Empty;
    }
}

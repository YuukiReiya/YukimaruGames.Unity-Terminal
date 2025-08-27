using System;

namespace YukimaruGames.Terminal.Domain.Model
{
    /// <summary>
    /// コマンドが持つメタ情報.
    /// </summary>
    public readonly struct CommandMeta : IEquatable<CommandMeta>
    {
        /// <summary>
        /// メソッドが持つ引数の数の最大数.
        /// </summary>
        /// <remarks>
        /// デフォルト引数の有無で必要な引数が変わるため.
        /// </remarks>
        public int MaxArgCount { get; }
        
        /// <summary>
        /// メソッドが持つ引数の数の最小数. 
        /// </summary>
        /// <remarks>
        /// デフォルト引数の有無で必要な引数が変わるため.
        /// </remarks>
        public int MinArgCount { get; }
        
        /// <summary>
        /// コマンド名.
        /// </summary>
        public string Command { get; }
        
        /// <summary>
        /// ヘルプ.
        /// </summary>
        public string Help { get; }

        public CommandMeta(string command, int maxArgCount, int minArgCount, string help)
        {
            Command = command;
            MaxArgCount = maxArgCount;
            MinArgCount = minArgCount;
            Help = help;
        }

        public bool Equals(CommandMeta other)
        {
            return
                MaxArgCount == other.MaxArgCount &&
                MinArgCount == other.MinArgCount &&
                Command == other.Command &&
                Help == other.Help;
        }

        public override bool Equals(object obj)
        {
            return obj is CommandMeta other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(MaxArgCount, MinArgCount, Command, Help);
        }

        public static bool operator ==(CommandMeta left, CommandMeta right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandMeta left, CommandMeta right)
        {
            return !left.Equals(right);
        }
    }
}

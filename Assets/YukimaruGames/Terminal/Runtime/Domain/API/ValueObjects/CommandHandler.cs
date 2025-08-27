using System;

namespace YukimaruGames.Terminal.Domain.Model
{
    /// <summary>
    /// コマンドのデリゲート.
    /// </summary>
    public delegate void CommandDelegate(CommandArgument[] args);
    
    /// <summary>
    /// コマンドのハンドラー.
    /// </summary>
    public readonly struct CommandHandler : IEquatable<CommandHandler>
    {
        /// <summary>
        /// プロシージャ.
        /// </summary>
        public readonly CommandDelegate Proc;
        
        /// <summary>
        /// メタ情報.
        /// </summary>
        public CommandMeta Meta { get; }
        
        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="proc">プロシージャ</param>
        /// <param name="commandName">登録コマンド名</param>
        /// <param name="minArgCount">メソッド引数の最小数</param>
        /// <param name="maxArgCount">メソッド引数の最大数</param>
        /// <param name="help">ヘルプテキスト</param>
        /// <exception cref="ArgumentNullException">
        /// <p>プロシージャにnullが渡された際の送出例外.</p>
        /// </exception>
        public CommandHandler(CommandDelegate proc, string commandName, int minArgCount, int maxArgCount, string help)
            : this(proc, new CommandMeta(commandName, maxArgCount, minArgCount, help))
        {
        }

        public CommandHandler(CommandDelegate proc, CommandMeta metadata)
        {
            Proc = proc ?? throw new ArgumentNullException(nameof(proc));
            Meta = metadata;
        }

        public bool Equals(CommandHandler other)
        {
            return Equals(Proc, other.Proc) && Meta.Equals(other.Meta);
        }

        public override bool Equals(object obj)
        {
            return obj is CommandHandler other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Proc, Meta);
        }

        public static bool operator ==(CommandHandler left, CommandHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandHandler left, CommandHandler right)
        {
            return !left.Equals(right);
        }
    }
}

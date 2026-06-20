using System;
using System.Threading;
using System.Threading.Tasks;

namespace YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects
{
    /// <summary>
    /// コマンドのデリゲート.
    /// </summary>
    public delegate void CommandDelegate(ReadOnlyMemory<CommandArgument> args);

    /// <summary>
    /// コマンドの非同期デリゲート.
    /// </summary>
    public delegate ValueTask CommandAsyncDelegate(ReadOnlyMemory<CommandArgument> args, CancellationToken cancellationToken);
    
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
        /// 非同期プロシージャ.
        /// </summary>
        public readonly CommandAsyncDelegate AsyncProc;
        
        /// <summary>
        /// メタ情報.
        /// </summary>
        public CommandMeta Meta { get; }

        /// <summary>
        /// 非同期コマンドとして登録されているか.
        /// </summary>
        public bool IsAsync => AsyncProc != null;
        
        /// <summary>
        /// 内部初期化用の共通プライベートコンストラクタ.
        /// </summary>
        private CommandHandler(CommandDelegate proc, CommandAsyncDelegate asyncProc, CommandMeta metadata)
        {
            if (proc == null && asyncProc == null)
            {
                throw new ArgumentNullException(null, "Either 'proc' or 'asyncProc' must be provided. Both cannot be null.");
            }

            Proc = proc;
            AsyncProc = asyncProc;
            Meta = metadata;
        }
        
        // ─── 同期 ───────────────────────────────────────────────────────────
        
        /// <summary>
        /// コンストラクタ(同期コマンド用).
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
            : this(proc, null, new CommandMeta(commandName, maxArgCount, minArgCount, help))
        {
        }

        /// <summary>
        /// コンストラクタ(同期コマンド用).
        /// </summary>
        /// <param name="proc">プロシージャ</param>
        /// <param name="metadata">メタ情報</param>
        /// <exception cref="ArgumentNullException">
        /// <p>プロシージャにnullが渡された際の送出例外.</p>
        /// </exception>
        public CommandHandler(CommandDelegate proc, CommandMeta metadata)
            : this(proc, null, metadata)
        {
        }

        // ─── 非同期 ───────────────────────────────────────────────────────────
        
        /// <summary>
        /// コンストラクタ(非同期コマンド用).
        /// </summary>
        /// <param name="asyncProc">非同期プロシージャ</param>
        /// <param name="commandName">登録コマンド名</param>
        /// <param name="minArgCount">メソッド引数の最小数</param>
        /// <param name="maxArgCount">メソッド引数の最大数</param>
        /// <param name="help">ヘルプテキスト</param>
        /// <exception cref="ArgumentNullException">
        /// <p>非同期プロシージャにnullが渡された際の送出例外.</p>
        /// </exception>
        public CommandHandler(CommandAsyncDelegate asyncProc, string commandName, int minArgCount, int maxArgCount, string help)
            : this(null, asyncProc, new CommandMeta(commandName, maxArgCount, minArgCount, help))
        {
        }

        /// <summary>
        /// コンストラクタ(非同期コマンド用).
        /// </summary>
        /// <param name="asyncProc">非同期プロシージャ</param>
        /// <param name="metadata">メタ情報</param>
        /// <exception cref="ArgumentNullException">
        /// <p>非同期プロシージャにnullが渡された際の送出例外.</p>
        /// </exception>
        public CommandHandler(CommandAsyncDelegate asyncProc, CommandMeta metadata)
            : this(null, asyncProc, metadata)
        {
        }

        public bool Equals(CommandHandler other)
        {
            return Equals(Proc, other.Proc) && Equals(AsyncProc, other.AsyncProc) && Meta.Equals(other.Meta);
        }

        public override bool Equals(object obj)
        {
            return obj is CommandHandler other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Proc, AsyncProc, Meta);
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
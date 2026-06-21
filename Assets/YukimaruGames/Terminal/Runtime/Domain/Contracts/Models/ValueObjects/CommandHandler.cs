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
        public CommandDelegate Proc { get; }

        /// <summary>
        /// 非同期プロシージャ.
        /// </summary>
        public CommandAsyncDelegate AsyncProc { get; }
        
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

        /// <summary>
        /// 現在のインスタンスと、指定された別のハンドラーが等価であるかどうかを判定します。
        /// </summary>
        /// <param name="other">比較対象のハンドラー</param>
        /// <returns>
        /// <p>二つのハンドラーが持つプロシージャ、非同期プロシージャ、およびメタ情報がすべて等しい場合は true。それ以外の場合は false。</p>
        /// </returns>
        public bool Equals(CommandHandler other)
        {
            return Equals(Proc, other.Proc) && Equals(AsyncProc, other.AsyncProc) && Meta.Equals(other.Meta);
        }

        /// <summary>
        /// 指定されたオブジェクトが現在のハンドラーと等価であるかどうかを判定します。
        /// </summary>
        /// <param name="obj">比較対象のオブジェクト</param>
        /// <returns>
        /// <p>対象が CommandHandler 構造体であり、かつ現在のインスタンスと等価である場合は true。それ以外の場合は false。</p>
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is CommandHandler other && Equals(other);
        }

        /// <summary>
        /// このインスタンスのハッシュコードを返します。
        /// </summary>
        /// <returns>
        /// <p>プロシージャ、非同期プロシージャ、およびメタ情報から計算された一意のハッシュ値。</p>
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Proc, AsyncProc, Meta);
        }

        /// <summary>
        /// 二つのハンドラーが等しいかどうかを判定します。
        /// </summary>
        /// <param name="left">左辺のハンドラー</param>
        /// <param name="right">右辺のハンドラー</param>
        /// <returns>
        /// <p>等しい場合は true。それ以外の場合は false。</p>
        /// </returns>
        public static bool operator ==(CommandHandler left, CommandHandler right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 二つのハンドラーが等しくないかどうかを判定します。
        /// </summary>
        /// <param name="left">左辺のハンドラー</param>
        /// <param name="right">右辺のハンドラー</param>
        /// <returns>
        /// <p>等しくない場合は true。等しい場合は false。</p>
        /// </returns>
        public static bool operator !=(CommandHandler left, CommandHandler right)
        {
            return !left.Equals(right);
        }
    }
}
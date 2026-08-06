using System;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Contracts.Attributes
{
    /// <summary>
    /// モード専用コマンド登録のためのカスタム属性.
    /// </summary>
    /// <remarks>
    /// <p>
    /// <see cref="TerminalCommandAttribute"/>(グローバルコマンド、staticのみ)とは別系統。
    /// こちらはインスタンスメソッドに付与でき、モードに入場している間だけ有効になる.
    /// </p>
    /// <p>
    /// <c>sealed</c>にしないのは<see cref="TerminalCommandAttribute"/>と同じ理由(ラッパー属性の許容)。
    /// <c>AllowMultiple = true</c> のため、1メソッドに複数の派生ラッパー属性を付与でき
    /// (例: 複数モードへ同じヘルプコマンドを割り当てる)、属性走査は複数形の
    /// <c>Attribute.GetCustomAttributes</c> を使う(単数形は<see cref="TerminalCommandAttribute"/>専用).
    /// </p>
    /// <p>
    /// <see cref="ModeType"/>と<see cref="ModeId"/>はどちらか一方だけが設定される。
    /// 両方を同時に指定できる公開コンストラクタは存在しないため、不正な状態はそもそも作れない.
    /// </p>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class TerminalModeCommandAttribute : Attribute
    {
        /// <summary>
        /// メタ情報.
        /// </summary>
        public CommandMeta Meta { get; }

        /// <summary>
        /// 対象モードの型(型参照で指定する場合). <see cref="ModeId"/>とは排他.
        /// </summary>
        public Type ModeType { get; }

        /// <summary>
        /// 対象モードの識別子(<see cref="Domain.Contracts.Modes.ITerminalMode.Id"/>と一致させる、
        /// asmdefを跨ぐ等で型参照を持てない場合用). <see cref="ModeType"/>とは排他.
        /// </summary>
        public string ModeId { get; }

        /// <summary>
        /// 型参照でモードを指定するコンストラクタ.
        /// </summary>
        public TerminalModeCommandAttribute(Type modeType, string command, int maxArgCount = 0, int minArgCount = -1, string help = "")
        {
            ModeType = modeType ?? throw new ArgumentNullException(nameof(modeType));
            ModeId = null;
            Meta = new CommandMeta(command, maxArgCount, minArgCount, help);
        }

        /// <summary>
        /// 識別子(文字列)でモードを指定するコンストラクタ.
        /// </summary>
        public TerminalModeCommandAttribute(string modeId, string command, int maxArgCount = 0, int minArgCount = -1, string help = "")
        {
            if (string.IsNullOrWhiteSpace(modeId))
            {
                throw new ArgumentException("modeId must not be null or empty.", nameof(modeId));
            }

            ModeId = modeId;
            ModeType = null;
            Meta = new CommandMeta(command, maxArgCount, minArgCount, help);
        }
    }
}

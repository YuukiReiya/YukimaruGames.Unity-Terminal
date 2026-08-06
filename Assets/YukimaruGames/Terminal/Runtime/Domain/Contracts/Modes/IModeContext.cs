using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// モードの実行中、そのモード自身(および、そのモード専用コマンド)からアクセス可能な機能一式.
    /// </summary>
    public interface IModeContext
    {
        /// <summary>
        /// このモード専用のコマンドレジストリ. <c>Add</c>で実行時に動的追加できる.
        /// </summary>
        ICommandRegistry Commands { get; }

        /// <summary>
        /// 出力用の窓口.
        /// </summary>
        IModeOutput Output { get; }

        /// <summary>
        /// モード遷移要求の窓口.
        /// </summary>
        IModeTransitionRequestSink Transitions { get; }

        /// <summary>
        /// モードスタックの読み取り専用ビュー.
        /// </summary>
        IModeStackInspector Stack { get; }
    }
}

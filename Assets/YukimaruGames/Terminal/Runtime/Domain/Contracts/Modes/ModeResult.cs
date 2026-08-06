namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// <see cref="ITerminalMode.HandleAsync"/> の入力継続に関する結果.
    /// </summary>
    /// <remarks>
    /// モード遷移(Push/Replace/Pop)はここに含めない。遷移要求は必ず
    /// <see cref="IModeTransitionRequestSink"/> 経由で行い、モードは
    /// <see cref="YukimaruGames.Terminal.Application.Interfaces.IExecuteCommandUseCase"/> が保持する
    /// モードスタックへ直接アクセスできない。
    /// </remarks>
    public enum ModeResult
    {
        /// <summary>
        /// 1行の入力を処理し終えた. 次の入力は新規の1行として扱われる.
        /// </summary>
        Continue = 0,

        /// <summary>
        /// 入力が未完結(複数行の継続入力待ち). 次の入力は継続行として蓄積される.
        /// </summary>
        NeedMoreInput = 1,
    }
}

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// <see cref="ITerminalMode.OnExitAsync"/> が呼ばれる理由.
    /// </summary>
    public enum ModeExitReason
    {
        /// <summary>
        /// 通常のPop(exitコマンド等)によって抜けた.
        /// </summary>
        /// <remarks>
        /// 割り込み(Ctrl+C相当)による退場は <see cref="Interrupted"/> が使われる。
        /// </remarks>
        Popped = 0,

        /// <summary>
        /// 別のモードに置き換えられた(Replace).
        /// </summary>
        Replaced = 1,

        /// <summary>
        /// 割り込み(Ctrl+C相当)によって抜けた.
        /// </summary>
        Interrupted = 2,

        /// <summary>
        /// アプリケーション/ターミナルの終了に伴い畳まれた.
        /// </summary>
        Shutdown = 3,

        /// <summary>
        /// <see cref="ITerminalMode.OnEnterAsync"/> が例外を投げて入場に失敗した.
        /// </summary>
        /// <remarks>
        /// 部分的に確保されたリソースの解放漏れを防ぐため、入場に失敗した場合も
        /// 対で <see cref="ITerminalMode.OnExitAsync"/> がこの理由で呼ばれる。
        /// 実装は「1行も初期化が完了していない」状態に対しても安全であること.
        /// </remarks>
        EnterFailed = 4,
    }
}

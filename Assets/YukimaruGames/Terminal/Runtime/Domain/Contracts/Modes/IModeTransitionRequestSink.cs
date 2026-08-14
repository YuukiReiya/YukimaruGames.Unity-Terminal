namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// モード遷移の要求を積む一方向の受け皿.
    /// </summary>
    /// <remarks>
    /// <p>
    /// モードは <c>ITerminalModeStack</c>(非公開)を直接操作できない。その代わり、このSinkに
    /// 「〜したい」という要求を積むだけに留め、実際の適用はディスパッチャがパイプライン境界
    /// (<see cref="ITerminalMode.HandleAsync"/> 等のコールバック完了後)で行う。
    /// </p>
    /// <p>
    /// 要求はモードのコールバック呼び出し区間(ターン)の外から呼ばれた場合、警告ログを出して
    /// 破棄される。同一ターン内で同一インスタンスへの重複Push等も破棄・警告の対象になる。
    /// </p>
    /// </remarks>
    public interface IModeTransitionRequestSink
    {
        /// <summary>
        /// 指定したモードへの遷移(現在のモードの上に積む)を要求する.
        /// </summary>
        void RequestPush(ITerminalMode mode);

        /// <summary>
        /// 現在のモードを指定したモードへ置き換えることを要求する.
        /// </summary>
        void RequestReplace(ITerminalMode mode);

        /// <summary>
        /// 現在のモードから抜けることを要求する.
        /// </summary>
        /// <param name="count">
        /// 抜ける段数. 現在の深さでクランプされ、<c>ExecutionMode</c>より下には絶対に行かない.
        /// </param>
        void RequestPop(int count = 1);
    }
}

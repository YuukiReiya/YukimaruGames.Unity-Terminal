namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// <see cref="ITerminalMode.OnInterrupt"/> の結果.
    /// </summary>
    public enum InterruptDisposition
    {
        /// <summary>
        /// モードが割り込みを自分で処理した(留まる).
        /// </summary>
        Handled = 0,

        /// <summary>
        /// モードを抜ける.
        /// </summary>
        Exit = 1,

        /// <summary>
        /// モードが割り込みに何の意見も持っていない. ディスパッチャの既定動作(即座にPop)が適用される.
        /// </summary>
        NotHandled = 2,
    }
}

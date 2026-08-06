namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// モード実行中の逐次出力用インターフェイス.
    /// </summary>
    /// <remarks>
    /// <see cref="YukimaruGames.Terminal.Application.Interfaces.ITerminalService"/> 丸ごとではなく、
    /// 出力用途に絞った狭いインターフェイス。理由は、モード実装から
    /// <c>ITerminalService.ExecuteAsync</c> 等を呼べてしまうと、ディスパッチャの排他ロックにより
    /// デッドロックするため(実行中に自分自身の実行を要求してしまう).
    /// </remarks>
    public interface IModeOutput
    {
        /// <summary>
        /// 通常ログの発行.
        /// </summary>
        void Message(string message);

        /// <summary>
        /// 警告ログの発行.
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// エラーログの発行.
        /// </summary>
        void Error(string message);
    }
}

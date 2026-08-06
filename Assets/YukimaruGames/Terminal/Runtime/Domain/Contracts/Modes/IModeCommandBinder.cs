using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// モードへの入場時、そのモード専用のコマンドレジストリを組み立てる.
    /// </summary>
    /// <remarks>
    /// 実装(<c>ModeCommandBinder</c>)はリフレクション/Expression Tree生成という
    /// Infrastructure層の関心事を持つため、そちらに配置される。
    /// Domain.Servicesはこの抽象だけを介して利用する.
    /// </remarks>
    public interface IModeCommandBinder
    {
        /// <summary>
        /// 指定したモードインスタンスへ束縛したコマンドレジストリを返す.
        /// </summary>
        ICommandRegistry BindFor(ITerminalMode mode);
    }
}

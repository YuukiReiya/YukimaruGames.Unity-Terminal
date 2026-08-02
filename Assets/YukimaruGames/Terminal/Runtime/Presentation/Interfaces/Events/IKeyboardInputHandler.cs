using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Events
{
    /// <summary>
    /// キーボード入力から<see cref="TerminalAction"/>の成立を判定するハンドラーの契約.
    /// </summary>
    public interface IKeyboardInputHandler
    {
        /// <summary>指定アクションがこのフレームで押下判定されたか.</summary>
        bool WasPressedThisFrame(TerminalAction action);
        /// <summary>指定アクションがこのフレームで解放判定されたか.</summary>
        bool WasReleasedThisFrame(TerminalAction action);
    }
}

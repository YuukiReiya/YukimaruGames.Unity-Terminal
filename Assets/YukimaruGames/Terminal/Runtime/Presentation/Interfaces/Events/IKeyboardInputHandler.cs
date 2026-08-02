using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Events
{
    /// <summary>
    /// キーボード入力から<see cref="TerminalAction"/>の成立を判定するハンドラーの契約.
    /// </summary>
    public interface IKeyboardInputHandler
    {
        /// <summary>
        /// 指定アクションが、そのアクションに設定された発火タイミング(押下/解放)でこのフレームに成立したか.
        /// </summary>
        bool WasTriggered(TerminalAction action);
    }
}

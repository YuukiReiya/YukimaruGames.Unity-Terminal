using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Models.Input
{
    public interface IInputKeyMap<out T>
    {
        T GetKey(TerminalAction action);

        /// <summary>
        /// 指定アクションの判定に必要な修飾キー.
        /// </summary>
        /// <remarks>修飾キーが不要なアクションは実装側の「なし」を表す値(例: Key.None)を返す.</remarks>
        T GetModifier(TerminalAction action);
    }
}

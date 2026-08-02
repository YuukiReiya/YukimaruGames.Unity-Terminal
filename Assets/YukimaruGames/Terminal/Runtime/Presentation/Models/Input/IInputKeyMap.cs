using System.Collections.Generic;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Presentation.Models.Input
{
    public interface IInputKeyMap<out T>
    {
        T GetKey(TerminalAction action);

        /// <summary>
        /// 指定アクションの判定に必要な修飾キー群.
        /// </summary>
        /// <remarks>修飾キーが不要なアクションは空を返す。順不同・重複なしを想定する.</remarks>
        IReadOnlyList<T> GetModifiers(TerminalAction action);
    }
}

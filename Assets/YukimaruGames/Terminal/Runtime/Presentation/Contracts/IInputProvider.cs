using System;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Contracts
{
    /// <summary>
    /// 入力イベントの通知元.
    /// <para>
    /// 描画（<see cref="Interfaces.Renderers.IInputRenderer"/>）とは責務を分離し、
    /// 入力に関するイベント通知のみを扱う契約。
    /// </para>
    /// </summary>
    public interface IInputProvider
    {
        /// <summary>入力文字の更新を通知.</summary>
        event Action<string> OnInputTextChanged;

        /// <summary>フォーカス状況の更新を通知.</summary>
        event Action<WindowFocus> OnFocusControlChanged;

        /// <summary>カーソル位置の終端トリガーの変更通知.</summary>
        event Action<bool> OnMoveCursorToEndTriggerChanged;

        /// <summary>
        /// IME変換状態の変更通知.
        /// </summary>
        /// <remarks>
        /// この値がtrueのときは「ユーザーがまだテキストの確定前(e.g.変換候補の選択など)」
        /// </remarks>
        event Action<bool> OnImeComposingStateChanged;
    }
}

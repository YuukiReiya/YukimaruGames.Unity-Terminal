using System;

namespace YukimaruGames.Terminal.UI.Input
{
    public interface IInputRenderer
    {
        /// <summary>
        /// 描画.
        /// </summary>
        void Render(InputRenderData data);

        /// <summary>
        /// 入力文字の更新を通知.
        /// </summary>
        event Action<string> OnInputTextChanged;

        /// <summary>
        /// フォーカス状況の更新を通知.
        /// </summary>
        event Action<Focus> OnFocusControlChanged;

        /// <summary>
        /// カーソル位置の終端トリガーの変更通知.
        /// </summary>
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

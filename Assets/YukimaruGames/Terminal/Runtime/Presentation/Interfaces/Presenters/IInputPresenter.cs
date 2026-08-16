using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Presenters
{
    public interface IInputPresenter : IInputRenderDataProvider
    {
        string InputText { get; }
        /// <summary>
        /// IME入力の変換状態か.
        /// </summary>
        bool IsImeComposing { get; }

        /// <summary>
        /// 入力欄が編集可能かどうか. falseの間は文字入力を受け付けない.
        /// </summary>
        bool IsEditable { get; set; }

        /// <summary>
        /// 入力欄がフォーカスを持っているか(=利用者が文字を打っている最中か).
        /// </summary>
        bool IsFocused { get; }

        void SetInputField(string inputText);
        void SetFocus(bool focus);
        void SetMoveCursorToEnd();
    }
}

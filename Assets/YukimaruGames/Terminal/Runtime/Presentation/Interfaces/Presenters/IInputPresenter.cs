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
        void SetInputField(string inputText);
        void SetFocus(bool focus);
        void SetMoveCursorToEnd();
    }
}

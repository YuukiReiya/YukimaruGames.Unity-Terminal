namespace YukimaruGames.Terminal.UI.Input
{
    public interface IInputPresenter : ITerminalInputRenderDataProvider
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

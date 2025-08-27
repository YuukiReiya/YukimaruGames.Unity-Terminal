using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public readonly struct TerminalInputRenderData
    {
        public string InputText { get; }
        public FocusControl FocusControl { get; }
        public bool IsMoveCursorToEnd { get; }

        public TerminalInputRenderData(string inputText,FocusControl focusControl,bool isMoveCursorToEnd)
        {
            InputText = inputText;
            FocusControl = focusControl;
            IsMoveCursorToEnd = isMoveCursorToEnd;
        }
    }
}

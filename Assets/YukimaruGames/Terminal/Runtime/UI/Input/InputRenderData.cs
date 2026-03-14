using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Input
{
    public readonly struct InputRenderData
    {
        public string InputText { get; }
        public FocusControl FocusControl { get; }
        public bool IsMoveCursorToEnd { get; }

        public InputRenderData(string inputText,FocusControl focusControl,bool isMoveCursorToEnd)
        {
            InputText = inputText;
            FocusControl = focusControl;
            IsMoveCursorToEnd = isMoveCursorToEnd;
        }
    }
}

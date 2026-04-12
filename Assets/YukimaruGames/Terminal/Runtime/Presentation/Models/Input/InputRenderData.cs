using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Models.Input
{
    public readonly struct InputRenderData
    {
        public string InputText { get; }
        public WindowFocus Focus { get; }
        public bool IsMoveCursorToEnd { get; }

        public InputRenderData(string inputText,WindowFocus focus,bool isMoveCursorToEnd)
        {
            InputText = inputText;
            Focus = focus;
            IsMoveCursorToEnd = isMoveCursorToEnd;
        }
    }
}

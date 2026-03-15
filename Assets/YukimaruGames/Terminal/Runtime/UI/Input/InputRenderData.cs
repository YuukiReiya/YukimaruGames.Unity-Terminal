using YukimaruGames.Terminal.UI.Core;

namespace YukimaruGames.Terminal.UI.Input
{
    public readonly struct InputRenderData
    {
        public string InputText { get; }
        public Focus Focus { get; }
        public bool IsMoveCursorToEnd { get; }

        public InputRenderData(string inputText,Focus focus,bool isMoveCursorToEnd)
        {
            InputText = inputText;
            Focus = focus;
            IsMoveCursorToEnd = isMoveCursorToEnd;
        }
    }
}

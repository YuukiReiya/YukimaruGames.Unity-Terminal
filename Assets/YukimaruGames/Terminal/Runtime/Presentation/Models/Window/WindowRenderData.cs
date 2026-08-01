using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Presentation.Models.Window
{
    public readonly struct WindowRenderData
    {
        public int Id { get; }
        public TerminalRect Rect { get; }

        public WindowRenderData(int id, TerminalRect rect)
        {
            Id = id;
            Rect = rect;
        }
    }
}

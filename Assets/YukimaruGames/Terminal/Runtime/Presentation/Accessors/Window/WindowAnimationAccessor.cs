using YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Accessors.Window
{
    public sealed class WindowAnimationAccessor : IWindowAnimationAccessor
    {
        public WindowState State { get; set; }
        public WindowAnchor Anchor { get; set; }
        public WindowStyle Style { get; set; }
        public float Duration { get; set; }
        public float Scale { get; set; }
    }
}

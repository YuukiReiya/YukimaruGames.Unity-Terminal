using System;
using YukimaruGames.Terminal.Presentation.Models.Launcher;

namespace YukimaruGames.Terminal.Presentation.Renderers.Launcher
{
    public interface ILauncherRenderer
    {
        event Action OnClickOpenButton;
        event Action OnClickCloseButton;
        void Render(LauncherRenderData renderData);
    }
}

using System;

namespace YukimaruGames.Terminal.UI.Launcher
{
    public interface ILauncherRenderer
    {
        event Action OnClickOpenButton;
        event Action OnClickCloseButton;
        void Render(LauncherRenderData renderData);
    }
}

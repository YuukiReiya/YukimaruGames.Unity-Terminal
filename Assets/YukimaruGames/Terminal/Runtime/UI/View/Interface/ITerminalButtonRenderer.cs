using System;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalButtonRenderer
    {
        event Action OnClickOpenButton;
        event Action OnClickCloseButton;
        void Render(TerminalButtonRenderData renderData);
    }
}

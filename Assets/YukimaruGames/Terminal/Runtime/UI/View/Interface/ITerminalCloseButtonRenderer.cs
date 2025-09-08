using System;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalCloseButtonRenderer
    {
        event Action OnClickButton;
        void Render(TerminalCloseButtonRenderData renderData);
    }
}

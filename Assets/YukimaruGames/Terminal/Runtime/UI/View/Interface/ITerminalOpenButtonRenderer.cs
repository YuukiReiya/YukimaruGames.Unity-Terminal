using System;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalOpenButtonRenderer
    {
        event Action OnClickCompactOpenButton;
        event Action OnClickFullOpenButton;
        void Render(TerminalOpenButtonRenderData renderData);
    }
}

using System;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalButtonRenderer
    {
        string ExecuteButtonText { get; }
        string CompactButtonText { get; }
        string FullButtonText { get; }
        void ExecuteButtonRender(TerminalButtonRenderData renderData);
        void OpenButtonsRender(TerminalButtonRenderData renderData);
        event Action OnClickExecuteButton;
        event Action OnClickCompactOpenButton;
        event Action OnClickFullOpenButton;
    }
}

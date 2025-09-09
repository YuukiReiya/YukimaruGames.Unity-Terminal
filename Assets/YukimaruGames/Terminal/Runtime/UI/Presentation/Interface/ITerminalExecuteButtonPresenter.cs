using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalExecuteButtonPresenter : ITerminalExecuteButtonRenderDataProvider
    {
        event Action OnExecuteTriggered;
    }
}

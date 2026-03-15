using System;

namespace YukimaruGames.Terminal.UI.Input
{
    public interface ISubmitPresenter : ISubmitRenderDataProvider
    {
        event Action OnExecuteTriggered;
    }
}

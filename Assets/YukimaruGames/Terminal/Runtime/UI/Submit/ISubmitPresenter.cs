using System;

namespace YukimaruGames.Terminal.UI.Submit
{
    public interface ISubmitPresenter : ISubmitRenderDataProvider
    {
        event Action OnExecuteTriggered;
    }
}

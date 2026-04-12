using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Presenters
{
    public interface ISubmitPresenter : ISubmitRenderDataProvider
    {
        event Action OnExecuteTriggered;
    }
}

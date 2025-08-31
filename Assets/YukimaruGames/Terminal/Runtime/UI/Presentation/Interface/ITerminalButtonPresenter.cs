using System;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI
{
    public interface ITerminalButtonPresenter : ITerminalButtonRenderDataProvider
    {
        event Action<bool> OnVisibleButtonChanged;
        event Action OnExecuteTriggered;
        void SetVisible(bool isVisible);
    }
}

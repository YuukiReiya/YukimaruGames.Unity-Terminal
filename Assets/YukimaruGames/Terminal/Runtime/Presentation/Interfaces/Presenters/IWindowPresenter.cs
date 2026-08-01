using System;
using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Presenters
{
    public interface IWindowPresenter : IWindowRenderDataProvider
    {
        bool IsAnimating { get; }
        TerminalRect Rect { get; }

        event Action<WindowState> OnCompleted;
        event Action<WindowState> OnAborted;

        void Open();
        void Close();

        void Refresh();
    }
}
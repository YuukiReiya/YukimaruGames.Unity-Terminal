using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Presenters.Window
{
    public interface IWindowPresenter : IWindowRenderDataProvider
    {
        bool IsAnimating { get; }
        Rect Rect { get; }

        event Action<WindowState> OnCompleted;
        event Action<WindowState> OnAborted;

        void Open();
        void Close();

        void Refresh();
    }
}
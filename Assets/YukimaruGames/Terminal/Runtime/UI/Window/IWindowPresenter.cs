using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
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
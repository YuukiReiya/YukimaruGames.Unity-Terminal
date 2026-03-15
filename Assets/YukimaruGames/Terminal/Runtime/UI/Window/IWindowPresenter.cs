using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
{
    public interface IWindowPresenter : IWindowRenderDataProvider
    {
        bool IsAnimating { get; }
        WindowState State { get; set; }
        WindowAnchor Anchor { get; set; }
        WindowStyle Style { get; set; }
        float Duration { set; }
        float Scale { set; }
        Rect Rect { get; }

        event Action<WindowState> OnCompleted;
        event Action<WindowState> OnAborted;

        void Open();
        void Close();

        void Refresh();
    }
}
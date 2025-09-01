using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalWindowPresenter : ITerminalWindowRenderDataProvider
    {
        bool IsAnimating { get; }
        TerminalState State { get; set; }
        TerminalAnchor Anchor { set; }
        TerminalWindowStyle Style { set; }
        float Duration { set; }
        float Scale { set; }
        Rect Rect { get; }
        
        event Action<TerminalState> OnCompleted;
        event Action<TerminalState> OnAborted;
        
        void Open();
        void Close();

        void Refresh();
    }
}

using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Accessors
{
    public interface IScrollAccessor :
        IScrollProvider,
        IScrollMutator
    {
        new Vector2 ScrollPosition { get; set; }

        /// <summary>
        /// <see cref="IScrollProvider.OnScrollChanged"/>を発火せずに保持値のみを同期する.
        /// </summary>
        void SyncPosition(Vector2 position);
    }

    public interface IScrollMutator
    {
        Vector2 ScrollPosition { set; }
        void ScrollToEnd();
    }
    
    public interface IScrollProvider
    {
        Vector2 ScrollPosition { get; }
        event Action<Vector2> OnScrollChanged;
    }
}

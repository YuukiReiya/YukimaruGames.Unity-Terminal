using System;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class ScrollConfigurator : IScrollConfigurator
    {
        private Vector2 _scrollPosition;

        public Vector2 ScrollPosition
        {
            get => _scrollPosition;
            set
            {
                if (_scrollPosition == value) return;

                _scrollPosition = value;
                OnScrollChanged?.Invoke(value);
            }
        }

        public event Action<Vector2> OnScrollChanged;

        public void ScrollToEnd()
        {
            if (Mathf.Approximately(_scrollPosition.y, float.MaxValue)) return;
            _scrollPosition.y = float.MaxValue;
            OnScrollChanged?.Invoke(_scrollPosition);
        }
    }
}

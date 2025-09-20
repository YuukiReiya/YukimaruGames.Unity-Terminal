using System;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Infrastructure
{
    public sealed class TerminalButtonVisibleContext : ITerminalButtonVisibleProvider, ITerminalButtonVisibleConfigurator
    {
        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetVisible(value);
        }

        private bool _isReverse;
        public bool IsReverse
        {
            get => _isReverse;
            set => SetReverse(value);
        }
        
        public event Action<bool> OnVisibleChanged;
        public event Action<bool> OnReverseChanged;

        private void SetVisible(bool isVisible)
        {
            if (_isVisible == isVisible) return;

            _isVisible = isVisible;
            OnVisibleChanged?.Invoke(isVisible);
        }

        private void SetReverse(bool isReverse)
        {
            if (_isReverse == isReverse) return;

            _isReverse = isReverse;
            OnReverseChanged?.Invoke(isReverse);
        }
    }
}

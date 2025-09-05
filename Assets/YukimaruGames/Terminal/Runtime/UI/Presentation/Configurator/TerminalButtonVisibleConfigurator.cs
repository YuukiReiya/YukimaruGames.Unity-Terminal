using System;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalButtonVisibleConfigurator : ITerminalButtonVisibleConfigurator, ITerminalButtonVisibleProvider, IDisposable
    {
        private bool _isVisible;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnVisibleChanged?.Invoke(value);
                }
            }
        }

        private bool _isReverse;

        public bool IsReverse
        {
            get => _isReverse;
            set
            {
                if (_isReverse != value)
                {
                    _isReverse = value;
                    OnReverseChanged?.Invoke(value);
                }
            }
        }


        public event Action<bool> OnVisibleChanged;
        public event Action<bool> OnReverseChanged;

        public void Dispose()
        {
            OnVisibleChanged = null;
            OnReverseChanged = null;
        }
    }
}

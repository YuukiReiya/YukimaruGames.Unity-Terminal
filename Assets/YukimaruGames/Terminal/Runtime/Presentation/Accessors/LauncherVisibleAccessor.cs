using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;

namespace YukimaruGames.Terminal.Presentation.Accessors
{
    public sealed class LauncherVisibleAccessor : ILauncherVisibleAccessor, IDisposable
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

        void IDisposable.Dispose()
        {
            OnVisibleChanged = null;
            OnReverseChanged = null;
        }
    }
}

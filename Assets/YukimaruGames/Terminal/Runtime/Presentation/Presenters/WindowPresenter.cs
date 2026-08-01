using System;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Providers;
using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window;
using YukimaruGames.Terminal.Presentation.Interfaces.Animators;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Window;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.SharedKernel.Mathematics;

namespace YukimaruGames.Terminal.Presentation.Presenters
{
    public sealed class WindowPresenter : IWindowPresenter, IUpdatable, IDisposable
    {
        private readonly IWindowAnimationAccessor _accessor;
        private readonly IWindowAnimator _windowAnimator;
        private readonly IScreenSizeProvider _screenSizeProvider;
        private readonly IExceptionLogger _exceptionLogger;

        private Action<WindowState> _onCompleted;
        private Action<WindowState> _onAborted;

        private float _elapsed;
        private float _duration;

        public bool IsAnimating { get; private set; }

        public TerminalRect Rect { get; private set; }

        public event Action<WindowState> OnCompleted
        {
            add => _onCompleted += value;
            remove => _onCompleted -= value;
        }

        public event Action<WindowState> OnAborted
        {
            add => _onAborted += value;
            remove => _onAborted -= value;
        }

        public WindowPresenter(
            IWindowAnimationAccessor accessor,
            IWindowAnimator animator,
            IScreenSizeProvider screenSizeProvider,
            IExceptionLogger exceptionLogger)
        {
            _accessor = accessor;
            _windowAnimator = animator;
            _screenSizeProvider = screenSizeProvider;
            _exceptionLogger = exceptionLogger;
        }

        public void Open()
        {
            if (IsAnimating) return;
            if (_accessor.State is WindowState.Open) return;
            _accessor.State = WindowState.Open;
            Play();
        }

        public void Close()
        {
            if (IsAnimating) return;
            if (_accessor.State is WindowState.Close) return;
            _accessor.State = WindowState.Close;
            Play();
        }

        public void Refresh()
        {
            if (IsAnimating) return;
            Evaluate(0f, 0f);
        }

        void IUpdatable.Update(float deltaTime)
        {
            if (!IsAnimating) return;

            _elapsed += deltaTime;

            if (_elapsed >= _duration)
            {
                Evaluate(_duration, _duration);
                IsAnimating = false;
                Invoke(_onCompleted, _accessor.State);
                return;
            }

            Evaluate(_duration, _elapsed);
        }

        private void Play()
        {
            _duration = _accessor.Duration;
            _elapsed = 0f;

            if (TerminalMath.Approximately(0f, _accessor.Duration))
            {
                IsAnimating = false;
                Evaluate(0f, 0f);
                Invoke(_onCompleted, _accessor.State);
                return;
            }

            IsAnimating = true;
            Evaluate(_duration, 0f);
        }

        private void Evaluate(float duration, float elapsed)
        {
            Rect = _windowAnimator.Evaluate(GetAnimatorData(duration, elapsed));
        }

        private WindowAnimatorData GetAnimatorData(float duration, float elapsed)
        {
            return new WindowAnimatorData(
                _screenSizeProvider.Size,
                _accessor.State, _accessor.Anchor, _accessor.Style, duration, _accessor.Scale, elapsed);
        }

        WindowRenderData IWindowRenderDataProvider.RenderData =>
            new WindowRenderData(GetHashCode(), Rect);

        void IDisposable.Dispose()
        {
            _onCompleted = null;
            _onAborted = null;
        }

        private void Invoke(Action<WindowState> action, WindowState arg)
        {
            try
            {
                action?.Invoke(arg);
            }
            catch (Exception e)
            {
                _exceptionLogger.Log(e);
            }
        }
    }
}

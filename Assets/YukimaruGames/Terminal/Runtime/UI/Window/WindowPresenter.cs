using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
{
    public sealed class WindowPresenter : IWindowPresenter, IDisposable
    {
        private readonly IWindowAnimatorDataConfigurator _configurator;
        private readonly IWindowAnimator _windowAnimator;
        private CancellationTokenSource _cts;

        private Action<WindowState> _onCompleted;
        private Action<WindowState> _onAborted;

        public bool IsAnimating { get; private set; }

        public WindowState State
        {
            get => _configurator.State;
            set => _configurator.State = value;
        }

        public WindowAnchor Anchor
        {
            get => _configurator.Anchor;
            set => _configurator.Anchor = value;
        }

        public WindowStyle Style
        {
            get => _configurator.Style;
            set => _configurator.Style = value;
        }

        public float Duration
        {
            get => _configurator.Duration;
            set => _configurator.Duration = value;
        }

        public float Scale
        {
            get => _configurator.Scale;
            set => _configurator.Scale = value;
        }

        public Rect Rect { get; private set; }

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
            IWindowAnimatorDataConfigurator configurator,
            IWindowAnimator animator)
        {
            _configurator = configurator;
            _windowAnimator = animator;
        }

        public void Open()
        {
            if (IsAnimating) return;
            if (State is WindowState.Open) return;
            State = WindowState.Open;
            Play();
        }

        public void Close()
        {
            if (IsAnimating) return;
            if (State is WindowState.Close) return;
            State = WindowState.Close;
            Play();
        }

        public void Refresh()
        {
            if (IsAnimating) return;
            Evaluate(0f, 0f);
        }

        private void Play()
        {
            _ = PlayAsync();
        }

        private async Task PlayAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (Mathf.Approximately(0f, Duration))
            {
                _cts = null;
                IsAnimating = false;
                Evaluate(0f, 0f);
                Invoke(_onCompleted, State);
                return;
            }

            _cts = new CancellationTokenSource();
            IsAnimating = true;

            try
            {
                var duration = Duration * Scale;
                var token = _cts.Token;
                var elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    token.ThrowIfCancellationRequested();
                    Evaluate(duration, elapsedTime);
                    await Task.Yield();
                    elapsedTime += Time.deltaTime;
                }

                Evaluate(duration, duration);
                Invoke(_onCompleted, State);
            }
            catch (OperationCanceledException)
            {
                Invoke(_onAborted, State);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                IsAnimating = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void Evaluate(float duration, float elapsed)
        {
            Rect = _windowAnimator.Evaluate(GetAnimatorData(duration, elapsed));
        }

        private WindowAnimatorData GetAnimatorData(float duration, float elapsed)
        {
            return new WindowAnimatorData(
                (Screen.width, Screen.height),
                State, Anchor, Style, duration, Scale, elapsed);
        }

        WindowRenderData IWindowRenderDataProvider.GetRenderData()
        {
            return new WindowRenderData(GetHashCode(), Rect);
        }

        public void Dispose()
        {
            _onCompleted = null;
            _onAborted = null;

            _cts?.Dispose();
            _cts = null;
        }

        private void Invoke(Action<WindowState> action, WindowState arg)
        {
            try
            {
                action?.Invoke(arg);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
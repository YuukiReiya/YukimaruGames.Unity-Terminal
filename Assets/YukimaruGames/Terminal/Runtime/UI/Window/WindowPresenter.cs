using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
{
    public sealed class WindowPresenter : IWindowPresenter, IDisposable
    {
        private readonly IAnimationDataAccessor _accessor;
        private readonly IWindowAnimator _windowAnimator;
        private CancellationTokenSource _cts;

        private Action<WindowState> _onCompleted;
        private Action<WindowState> _onAborted;

        public bool IsAnimating { get; private set; }

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
            IAnimationDataAccessor accessor,
            IWindowAnimator animator)
        {
            _accessor = accessor;
            _windowAnimator = animator;
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

        private void Play()
        {
            _ = PlayAsync();
        }

        private async Task PlayAsync()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (Mathf.Approximately(0f, _accessor.Duration))
            {
                _cts = null;
                IsAnimating = false;
                Evaluate(0f, 0f);
                Invoke(_onCompleted, _accessor.State);
                return;
            }

            _cts = new CancellationTokenSource();
            IsAnimating = true;

            try
            {
                var duration = _accessor.Duration * _accessor.Scale;
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
                Invoke(_onCompleted, _accessor.State);
            }
            catch (OperationCanceledException)
            {
                Invoke(_onAborted, _accessor.State);
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
                _accessor.State, _accessor.Anchor, _accessor.Style, duration, _accessor.Scale, elapsed);
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
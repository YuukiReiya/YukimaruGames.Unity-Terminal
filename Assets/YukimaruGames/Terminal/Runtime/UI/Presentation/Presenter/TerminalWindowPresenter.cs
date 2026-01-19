using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalWindowPresenter : ITerminalWindowPresenter, IDisposable
    {
        private readonly ITerminalWindowAnimatorDataMutator _mutator;
        private readonly ITerminalWindowAnimator _windowAnimator;
        private CancellationTokenSource _cts;

        private Action<TerminalState> _onCompleted;
        private Action<TerminalState> _onAborted;

        public bool IsAnimating { get; private set; }

        public TerminalState State
        {
            get => _mutator.State;
            set => _mutator.State = value;
        }

        public TerminalAnchor Anchor
        {
            get => _mutator.Anchor;
            set => _mutator.Anchor = value;
        }

        public TerminalWindowStyle Style
        {
            get => _mutator.Style;
            set => _mutator.Style = value;
        }

        public float Duration
        {
            get => _mutator.Duration;
            set => _mutator.Duration = value;
        }

        public float Scale
        {
            get => _mutator.Scale;
            set => _mutator.Scale = value;
        }

        public Rect Rect { get; private set; }

        public event Action<TerminalState> OnCompleted
        {
            add => _onCompleted += value;
            remove => _onCompleted -= value;
        }

        public event Action<TerminalState> OnAborted
        {
            add => _onAborted += value;
            remove => _onAborted -= value;
        }

        public TerminalWindowPresenter(
            ITerminalWindowAnimatorDataMutator mutator,
            ITerminalWindowAnimator animator)
        {
            _mutator = mutator;
            _windowAnimator = animator;
        }

        public void Open()
        {
            if (IsAnimating) return;
            if (State is TerminalState.Open) return;
            State = TerminalState.Open;
            Play();
        }

        public void Close()
        {
            if (IsAnimating) return;
            if (State is TerminalState.Close) return;
            State = TerminalState.Close;
            Play();
        }

        public void Refresh()
        {
            if (IsAnimating) return;
            Evaluate(0f, 0f);
        }

        private async void Play()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (Mathf.Approximately(0f, Duration))
            {
                _cts?.Dispose();
                _cts = null;
                IsAnimating = false;
                Evaluate(0f, 0f);
                _onCompleted?.Invoke(State);
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
                _onCompleted?.Invoke(State);
            }
            catch (OperationCanceledException)
            {
                _onAborted?.Invoke(State);
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

        private TerminalWindowAnimatorData GetAnimatorData(float duration, float elapsed)
        {
            return new TerminalWindowAnimatorData(
                (Screen.width, Screen.height),
                State, Anchor, Style, duration, Scale, elapsed);
        }

        TerminalWindowRenderData ITerminalWindowRenderDataProvider.GetRenderData()
        {
            return new TerminalWindowRenderData(GetHashCode(), Rect);
        }

        public void Dispose()
        {
            _onCompleted = null;
            _onAborted = null;

            _cts?.Dispose();
            _cts = null;
        }
    }
}

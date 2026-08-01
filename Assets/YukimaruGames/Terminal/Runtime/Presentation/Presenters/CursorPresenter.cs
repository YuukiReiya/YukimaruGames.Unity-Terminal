using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Presentation.Presenters
{
    /// <summary>
    /// カーソルの点滅状態を管理するPresenter.
    /// </summary>
    public sealed class CursorPresenter : ICursorPresenter, IUpdatable
    {
        private const float HalfPeriodsPerCycle = 2f;

        private readonly ICursorFlashSpeedProvider _flashSpeedProvider;
        private readonly ICursorView _cursorView;

        private float _elapsed;

        public bool IsVisible { get; private set; } = true;

        /// <param name="flashSpeedProvider">点滅速度の提供者.</param>
        /// <param name="cursorView">
        /// 表示状態の変化を通知するView（省略可）。
        /// 指定した場合、<see cref="IsVisible"/>が変化するたびに<see cref="ICursorView.SetVisible"/>を呼び出す。
        /// </param>
        public CursorPresenter(ICursorFlashSpeedProvider flashSpeedProvider, ICursorView cursorView = null)
        {
            _flashSpeedProvider = flashSpeedProvider;
            _cursorView = cursorView;
        }

        void IUpdatable.Update(float deltaTime)
        {
            var flashSpeed = _flashSpeedProvider.FlashSpeed;
            if (flashSpeed <= 0f)
            {
                SetVisible(true);
                _elapsed = 0f;
                return;
            }

            var halfPeriod = 1f / (flashSpeed * HalfPeriodsPerCycle);

            _elapsed += deltaTime;
            var elapsedHalfPeriods = (int)(_elapsed / halfPeriod);
            if (elapsedHalfPeriods == 0) return;

            _elapsed -= elapsedHalfPeriods * halfPeriod;
            if (elapsedHalfPeriods % 2 != 0)
            {
                SetVisible(!IsVisible);
            }
        }

        private void SetVisible(bool visible)
        {
            if (IsVisible == visible) return;

            IsVisible = visible;
            _cursorView?.SetVisible(visible);
        }
    }
}

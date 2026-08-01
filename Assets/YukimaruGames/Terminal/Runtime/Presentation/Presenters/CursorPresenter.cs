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

        private float _elapsed;

        public bool IsVisible { get; private set; } = true;

        public CursorPresenter(ICursorFlashSpeedProvider flashSpeedProvider)
        {
            _flashSpeedProvider = flashSpeedProvider;
        }

        void IUpdatable.Update(float deltaTime)
        {
            var flashSpeed = _flashSpeedProvider.FlashSpeed;
            if (flashSpeed <= 0f)
            {
                IsVisible = true;
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
                IsVisible = !IsVisible;
            }
        }
    }
}

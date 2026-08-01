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

            var halfPeriod = 1f / (flashSpeed * 2f);

            _elapsed += deltaTime;
            if (_elapsed < halfPeriod) return;

            _elapsed -= halfPeriod;
            IsVisible = !IsVisible;
        }
    }
}

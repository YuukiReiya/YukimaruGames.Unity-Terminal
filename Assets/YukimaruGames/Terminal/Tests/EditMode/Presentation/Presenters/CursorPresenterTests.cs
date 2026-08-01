using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Presentation.Presenters
{
    [TestFixture]
    public sealed class CursorPresenterTests
    {
        private sealed class StubCursorFlashSpeedProvider : ICursorFlashSpeedProvider
        {
            public float FlashSpeed { get; set; }
            public event Action<float> OnChangedFlashSpeed;
        }

        [Test]
        public void IsVisible_Initially_ReturnsTrue()
        {
            // Arrange
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 1f };
            var presenter = new CursorPresenter(provider);

            // Assert
            Assert.IsTrue(presenter.IsVisible);
        }

        [Test]
        public void Update_FlashSpeedZero_AlwaysVisible()
        {
            // Arrange
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 0f };
            var presenter = new CursorPresenter(provider);
            var updatable = (IUpdatable)presenter;

            // Act
            updatable.Update(10f);

            // Assert
            Assert.IsTrue(presenter.IsVisible);
        }

        [Test]
        public void Update_ElapsedReachesHalfPeriod_TogglesVisibility()
        {
            // Arrange: FlashSpeed=1 -> halfPeriod = 1 / (1 * 2) = 0.5s
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 1f };
            var presenter = new CursorPresenter(provider);
            var updatable = (IUpdatable)presenter;

            // Act
            updatable.Update(0.5f);

            // Assert
            Assert.IsFalse(presenter.IsVisible);
        }

        [Test]
        public void Update_BeforeHalfPeriod_DoesNotToggle()
        {
            // Arrange
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 1f };
            var presenter = new CursorPresenter(provider);
            var updatable = (IUpdatable)presenter;

            // Act
            updatable.Update(0.1f);

            // Assert
            Assert.IsTrue(presenter.IsVisible);
        }

        [Test]
        public void Update_MultipleHalfPeriods_TogglesBackToVisible()
        {
            // Arrange
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 1f };
            var presenter = new CursorPresenter(provider);
            var updatable = (IUpdatable)presenter;

            // Act
            updatable.Update(0.5f);
            updatable.Update(0.5f);

            // Assert
            Assert.IsTrue(presenter.IsVisible);
        }
    }
}

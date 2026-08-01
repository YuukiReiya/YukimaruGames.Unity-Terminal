using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Presentation.Presenters
{
    /// <summary>
    /// <see cref="CursorPresenter"/>のカーソル点滅状態を検証する.
    /// </summary>
    [TestFixture]
    public sealed class CursorPresenterTests
    {
        private sealed class StubCursorFlashSpeedProvider : ICursorFlashSpeedProvider
        {
            public float FlashSpeed { get; set; }

            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<float> OnChangedFlashSpeed { add { } remove { } }
        }

        /// <summary>初期状態でIsVisibleがtrueであることを検証する.</summary>
        [Test]
        public void IsVisible_Initially_ReturnsTrue()
        {
            // Arrange
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 1f };
            var presenter = new CursorPresenter(provider);

            // Assert
            Assert.IsTrue(presenter.IsVisible);
        }

        /// <summary>FlashSpeedが0の場合は常にIsVisibleがtrueのままであることを検証する.</summary>
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

        /// <summary>経過時間がhalfPeriodに達するとIsVisibleが反転することを検証する.</summary>
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

        /// <summary>経過時間がhalfPeriod未満の場合はIsVisibleが反転しないことを検証する.</summary>
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

        /// <summary>halfPeriod2回分を2回のUpdateで消化するとIsVisibleが元の状態に戻ることを検証する.</summary>
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

        /// <summary>1回のUpdateで奇数回のhalfPeriodが経過した場合、IsVisibleが1回だけ反転することを検証する.</summary>
        [Test]
        public void Update_SingleUpdateSpansMultipleHalfPeriods_TogglesOnce()
        {
            // Arrange: FlashSpeed=1 -> halfPeriod = 0.5s。deltaTime=1.5sは3半周期分(奇数)に相当する.
            var provider = new StubCursorFlashSpeedProvider { FlashSpeed = 1f };
            var presenter = new CursorPresenter(provider);
            var updatable = (IUpdatable)presenter;

            // Act
            updatable.Update(1.5f);

            // Assert
            Assert.IsFalse(presenter.IsVisible);
        }
    }
}

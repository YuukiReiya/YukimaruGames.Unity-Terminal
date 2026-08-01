using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Tests.EditMode.Presentation.Presenters
{
    [TestFixture]
    public sealed class InputPresenterTests
    {
        private sealed class StubInputProvider : IInputProvider
        {
            public event Action<string> OnInputTextChanged;
            public event Action<WindowFocus> OnFocusControlChanged;
            public event Action<bool> OnMoveCursorToEndTriggerChanged;
            public event Action<bool> OnImeComposingStateChanged;

            public void RaiseInputTextChanged(string text) => OnInputTextChanged?.Invoke(text);
            public void RaiseFocusControlChanged(WindowFocus focus) => OnFocusControlChanged?.Invoke(focus);
            public void RaiseMoveCursorToEndTriggerChanged(bool moveCursorToEnd) => OnMoveCursorToEndTriggerChanged?.Invoke(moveCursorToEnd);
            public void RaiseImeComposingStateChanged(bool isComposing) => OnImeComposingStateChanged?.Invoke(isComposing);
        }

        [Test]
        public void Constructor_SetsBootupCommand()
        {
            // Arrange & Act
            var presenter = new InputPresenter(new StubInputProvider(), "help");

            // Assert
            Assert.AreEqual("help", presenter.InputText);
        }

        [Test]
        public void OnInputTextChanged_UpdatesInputText()
        {
            // Arrange
            var provider = new StubInputProvider();
            var presenter = new InputPresenter(provider, string.Empty);

            // Act
            provider.RaiseInputTextChanged("echo hello");

            // Assert
            Assert.AreEqual("echo hello", presenter.InputText);
        }

        [Test]
        public void OnImeComposingStateChanged_UpdatesIsImeComposing()
        {
            // Arrange
            var provider = new StubInputProvider();
            var presenter = new InputPresenter(provider, string.Empty);

            // Act
            provider.RaiseImeComposingStateChanged(true);

            // Assert
            Assert.IsTrue(presenter.IsImeComposing);
        }

        [Test]
        public void OnMoveCursorToEndTriggerChanged_UpdatesRenderData()
        {
            // Arrange
            var provider = new StubInputProvider();
            var presenter = new InputPresenter(provider, string.Empty);
            var renderDataProvider = (IInputRenderDataProvider)presenter;

            // Act
            provider.RaiseMoveCursorToEndTriggerChanged(true);

            // Assert
            Assert.IsTrue(renderDataProvider.RenderData.IsMoveCursorToEnd);
        }

        [Test]
        public void Dispose_UnsubscribesFromInputProvider()
        {
            // Arrange
            var provider = new StubInputProvider();
            var presenter = new InputPresenter(provider, string.Empty);
            ((IDisposable)presenter).Dispose();

            // Act
            provider.RaiseInputTextChanged("should not apply");

            // Assert
            Assert.AreEqual(string.Empty, presenter.InputText);
        }
    }
}

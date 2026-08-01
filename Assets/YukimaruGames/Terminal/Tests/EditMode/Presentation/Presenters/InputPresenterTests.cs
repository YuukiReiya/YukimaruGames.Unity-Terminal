using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Tests.EditMode.Presentation.Presenters
{
    /// <summary>
    /// <see cref="InputPresenter"/>の入力状態管理を検証する.
    /// </summary>
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

        /// <summary>コンストラクタに渡した起動コマンドが初期入力テキストに設定されることを検証する.</summary>
        [Test]
        public void Constructor_SetsBootupCommand()
        {
            // Arrange & Act
            var presenter = new InputPresenter(new StubInputProvider(), "help");

            // Assert
            Assert.AreEqual("help", presenter.InputText);
        }

        /// <summary>編集可能な状態で入力変更通知を受けるとInputTextが更新されることを検証する.</summary>
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

        /// <summary>編集不可の状態で入力変更通知を受けるとInputTextが空文字列になることを検証する.</summary>
        [Test]
        public void OnInputTextChanged_ClearsInputText_WhenNotEditable()
        {
            // Arrange
            var provider = new StubInputProvider();
            var presenter = new InputPresenter(provider, string.Empty)
            {
                IsEditable = false,
            };

            // Act
            provider.RaiseInputTextChanged("echo hello");

            // Assert
            Assert.AreEqual(string.Empty, presenter.InputText);
        }

        /// <summary>IME変換中フラグの変更通知を受けるとIsImeComposingが更新されることを検証する.</summary>
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

        /// <summary>カーソル終端移動トリガーの変更通知を受けるとRenderDataに反映されることを検証する.</summary>
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

        /// <summary>Dispose後はInputProviderのイベント購読が解除され、通知を受けても状態が変化しないことを検証する.</summary>
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

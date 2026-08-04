using NUnit.Framework;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Composition.Input.InputSystem;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Tests.PlayMode.Composition.Input
{
    /// <summary>
    /// <see cref="InputSystemKeyboardHandler"/>の入力判定を<see cref="InputTestFixture"/>経由で検証する.
    /// </summary>
    /// <remarks>
    /// Issue #105: macOSの<c>CGEventPost</c>によるOSレベルのキー入力注入は、CI/自動検証環境において
    /// 断続的にUnity Editorへ配信されず、実キー入力を用いた検証手法自体が不安定であることが判明した。
    /// <see cref="InputTestFixture"/>は仮想デバイスへ直接イベントを注入するため、OS側のHIDイベントタップを
    /// 経由せず、Input System側の入力判定ロジックのみを決定的に検証できる。
    /// </remarks>
    [TestFixture]
    public sealed class InputSystemKeyboardHandlerTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private InputSystemKeyboardHandler _handler;

        public override void Setup()
        {
            base.Setup();

            _keyboard = InputSystem.AddDevice<Keyboard>();
            _handler = new InputSystemKeyboardHandler(
                new InputSystemKey(),
                new TerminalActionTriggerTiming(),
                new TerminalActionPriority());
        }

        /// <summary>既定設定でEnterキーを押下すると、Executeアクションが発火することを検証する.</summary>
        [Test]
        public void WasTriggered_Execute_PressEnter_ReturnsTrue()
        {
            Press(_keyboard.enterKey);

            Assert.IsTrue(_handler.WasTriggered(TerminalAction.Execute));
        }

        /// <summary>Enterキーを押下していないフレームでは、Executeアクションが発火しないことを検証する.</summary>
        [Test]
        public void WasTriggered_Execute_NoInput_ReturnsFalse()
        {
            Assert.IsFalse(_handler.WasTriggered(TerminalAction.Execute));
        }

        /// <summary>Enterキー押下の翌フレームでは、押下フレームのみ発火する(継続発火しない)ことを検証する.</summary>
        [Test]
        public void WasTriggered_Execute_HeldNextFrame_ReturnsFalse()
        {
            Press(_keyboard.enterKey);
            Assert.IsTrue(_handler.WasTriggered(TerminalAction.Execute));

            InputSystem.Update();

            Assert.IsFalse(_handler.WasTriggered(TerminalAction.Execute));
        }

        /// <summary>既定設定でCtrl+Cを押下すると、Cancelアクションが発火することを検証する.</summary>
        [Test]
        public void WasTriggered_Cancel_PressCtrlC_ReturnsTrue()
        {
            Press(_keyboard.leftCtrlKey);
            Press(_keyboard.cKey);

            Assert.IsTrue(_handler.WasTriggered(TerminalAction.Cancel));
        }

        /// <summary>修飾キーなしでCを押下しても、Cancelアクションが発火しないことを検証する.</summary>
        [Test]
        public void WasTriggered_Cancel_PressCWithoutModifier_ReturnsFalse()
        {
            Press(_keyboard.cKey);

            Assert.IsFalse(_handler.WasTriggered(TerminalAction.Cancel));
        }
    }
}

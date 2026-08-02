using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Tests.EditMode.Presentation.Models.Event
{
    /// <summary>
    /// <see cref="TerminalActionTriggerTiming"/>の既定値と異常入力に対する契約を検証する.
    /// </summary>
    [TestFixture]
    public sealed class TerminalActionTriggerTimingTests
    {
        private TerminalActionTriggerTiming _timing;

        /// <summary>各テスト実行前に既定設定の<see cref="TerminalActionTriggerTiming"/>を生成する.</summary>
        [SetUp]
        public void SetUp()
        {
            _timing = new TerminalActionTriggerTiming();
        }

        /// <summary>OpenとCloseの既定タイミングはReleasedであることを検証する.</summary>
        [TestCase(TerminalAction.Open)]
        [TestCase(TerminalAction.Close)]
        public void GetTiming_OpenAndClose_DefaultsToReleased(TerminalAction action)
        {
            Assert.AreEqual(TerminalActionTriggerTiming.Timing.Released, _timing.GetTiming(action));
        }

        /// <summary>Open/Close以外のアクションの既定タイミングはPressedであることを検証する.</summary>
        [TestCase(TerminalAction.Execute)]
        [TestCase(TerminalAction.Cancel)]
        [TestCase(TerminalAction.PreviousHistory)]
        [TestCase(TerminalAction.NextHistory)]
        [TestCase(TerminalAction.Autocomplete)]
        [TestCase(TerminalAction.Focus)]
        public void GetTiming_OtherActions_DefaultsToPressed(TerminalAction action)
        {
            Assert.AreEqual(TerminalActionTriggerTiming.Timing.Pressed, _timing.GetTiming(action));
        }

        /// <summary>未定義の<see cref="TerminalAction"/>を渡すと拒否することを検証する.</summary>
        [Test]
        public void GetTiming_UndefinedAction_ThrowsArgumentOutOfRangeException()
        {
            var undefined = (TerminalAction)(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => _timing.GetTiming(undefined));
        }
    }
}

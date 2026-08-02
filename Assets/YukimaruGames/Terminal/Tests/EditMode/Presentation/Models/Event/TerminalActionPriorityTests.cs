using System.Collections.Generic;
using NUnit.Framework;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Tests.EditMode.Presentation.Models.Event
{
    /// <summary>
    /// <see cref="TerminalActionPriority"/>の優先度判定ロジックを検証する.
    /// </summary>
    [TestFixture]
    public sealed class TerminalActionPriorityTests
    {
        /// <summary>単独で成立しているアクションは常に最優先と判定されることを検証する.</summary>
        [Test]
        public void IsHighestPriority_SingleAction_ReturnsTrue()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.Execute };

            Assert.IsTrue(TerminalActionPriority.IsHighestPriority(TerminalAction.Execute, satisfied));
        }

        /// <summary>CancelとExecuteが同時に成立している場合、Cancelだけが最優先になることを検証する.</summary>
        [Test]
        public void IsHighestPriority_CancelAndExecute_OnlyCancelWins()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.Cancel, TerminalAction.Execute };

            Assert.IsTrue(TerminalActionPriority.IsHighestPriority(TerminalAction.Cancel, satisfied));
            Assert.IsFalse(TerminalActionPriority.IsHighestPriority(TerminalAction.Execute, satisfied));
        }

        /// <summary>CloseとOpenが同時に成立している場合、negative側のCloseが優先されることを検証する.</summary>
        [Test]
        public void IsHighestPriority_CloseAndOpen_CloseWins()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.Close, TerminalAction.Open };

            Assert.IsTrue(TerminalActionPriority.IsHighestPriority(TerminalAction.Close, satisfied));
            Assert.IsFalse(TerminalActionPriority.IsHighestPriority(TerminalAction.Open, satisfied));
        }

        /// <summary>全8アクションが同時に成立していても、勝者は常にちょうど1つだけであることを検証する.</summary>
        [Test]
        public void IsHighestPriority_AllActionsSatisfied_ExactlyOneWins()
        {
            var satisfied = new List<TerminalAction>
            {
                TerminalAction.Open, TerminalAction.Close, TerminalAction.Execute, TerminalAction.Cancel,
                TerminalAction.PreviousHistory, TerminalAction.NextHistory, TerminalAction.Autocomplete, TerminalAction.Focus
            };

            var winnerCount = 0;
            foreach (var action in satisfied)
            {
                if (TerminalActionPriority.IsHighestPriority(action, satisfied)) winnerCount++;
            }

            Assert.AreEqual(1, winnerCount);
            Assert.IsTrue(TerminalActionPriority.IsHighestPriority(TerminalAction.Cancel, satisfied));
        }

        /// <summary>
        /// <see cref="TerminalAction.None"/>は優先度テーブルに存在しないため拒否される(契約違反)ことを検証する.
        /// </summary>
        [Test]
        public void IsHighestPriority_ActionIsNone_ThrowsKeyNotFoundException()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.None };

            Assert.Throws<KeyNotFoundException>(() =>
                TerminalActionPriority.IsHighestPriority(TerminalAction.None, satisfied));
        }
    }
}

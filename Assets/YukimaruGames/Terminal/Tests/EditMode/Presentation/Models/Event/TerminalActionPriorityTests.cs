using System;
using System.Collections.Generic;
using System.Reflection;
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
        private TerminalActionPriority _priority;

        /// <summary>各テスト実行前に既定設定の<see cref="TerminalActionPriority"/>を生成する.</summary>
        [SetUp]
        public void SetUp()
        {
            _priority = new TerminalActionPriority();
        }

        /// <summary>単独で成立しているアクションは常に最優先と判定されることを検証する.</summary>
        [Test]
        public void IsHighestPriority_SingleAction_ReturnsTrue()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.Execute };

            Assert.IsTrue(_priority.IsHighestPriority(TerminalAction.Execute, satisfied));
        }

        /// <summary>CancelとExecuteが同時に成立している場合、Cancelだけが最優先になることを検証する.</summary>
        [Test]
        public void IsHighestPriority_CancelAndExecute_OnlyCancelWins()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.Cancel, TerminalAction.Execute };

            Assert.IsTrue(_priority.IsHighestPriority(TerminalAction.Cancel, satisfied));
            Assert.IsFalse(_priority.IsHighestPriority(TerminalAction.Execute, satisfied));
        }

        /// <summary>CloseとOpenが同時に成立している場合、negative側のCloseが優先されることを検証する.</summary>
        [Test]
        public void IsHighestPriority_CloseAndOpen_CloseWins()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.Close, TerminalAction.Open };

            Assert.IsTrue(_priority.IsHighestPriority(TerminalAction.Close, satisfied));
            Assert.IsFalse(_priority.IsHighestPriority(TerminalAction.Open, satisfied));
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
                if (_priority.IsHighestPriority(action, satisfied)) winnerCount++;
            }

            Assert.AreEqual(1, winnerCount);
            Assert.IsTrue(_priority.IsHighestPriority(TerminalAction.Cancel, satisfied));
        }

        /// <summary>
        /// <see cref="TerminalAction.None"/>は優先度テーブルに存在しないため拒否される(契約違反)ことを検証する.
        /// </summary>
        [Test]
        public void IsHighestPriority_ActionIsNone_ThrowsArgumentOutOfRangeException()
        {
            var satisfied = new List<TerminalAction> { TerminalAction.None };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _priority.IsHighestPriority(TerminalAction.None, satisfied));
        }

        /// <summary>
        /// Inspector編集によりOpenとCloseに同じ優先度が設定されても、enum宣言順によるタイブレークで
        /// 常にちょうど1つの勝者(宣言順で先に来るOpen)が決まることを検証する.
        /// </summary>
        [Test]
        public void IsHighestPriority_TiedPriority_TieBreaksByDeclarationOrder()
        {
            SetPriority(_priority, "_open", 5);
            SetPriority(_priority, "_close", 5);

            var satisfied = new List<TerminalAction> { TerminalAction.Open, TerminalAction.Close };

            Assert.IsTrue(_priority.IsHighestPriority(TerminalAction.Open, satisfied));
            Assert.IsFalse(_priority.IsHighestPriority(TerminalAction.Close, satisfied));
        }

        private static void SetPriority(TerminalActionPriority priority, string fieldName, int value)
        {
            var field = typeof(TerminalActionPriority).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(priority, value);
        }
    }
}

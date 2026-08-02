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
        /// 優先順位の並び替え(ReorderableList相当)を反映して、順序の先頭にあるアクションが
        /// 優先されることを検証する.
        /// </summary>
        [Test]
        public void IsHighestPriority_ReorderedOpenBeforeClose_OpenWins()
        {
            SetOrder(_priority, TerminalAction.Open, TerminalAction.Close, TerminalAction.Execute,
                TerminalAction.Cancel, TerminalAction.PreviousHistory, TerminalAction.NextHistory,
                TerminalAction.Autocomplete, TerminalAction.Focus);

            var satisfied = new List<TerminalAction> { TerminalAction.Open, TerminalAction.Close };

            Assert.IsTrue(_priority.IsHighestPriority(TerminalAction.Open, satisfied));
            Assert.IsFalse(_priority.IsHighestPriority(TerminalAction.Close, satisfied));
        }

        /// <summary>
        /// 優先順位配列に対象アクションが含まれていない(不正な構成)場合は拒否されることを検証する.
        /// </summary>
        [Test]
        public void IsHighestPriority_ActionMissingFromOrder_ThrowsArgumentOutOfRangeException()
        {
            SetOrder(_priority, TerminalAction.Cancel, TerminalAction.Close, TerminalAction.Open,
                TerminalAction.Execute, TerminalAction.PreviousHistory, TerminalAction.NextHistory,
                TerminalAction.Autocomplete);

            var satisfied = new List<TerminalAction> { TerminalAction.Focus };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _priority.IsHighestPriority(TerminalAction.Focus, satisfied));
        }

        private static void SetOrder(TerminalActionPriority priority, params TerminalAction[] order)
        {
            var field = typeof(TerminalActionPriority).GetField("_order", BindingFlags.NonPublic | BindingFlags.Instance);
            field!.SetValue(priority, order);
        }
    }
}

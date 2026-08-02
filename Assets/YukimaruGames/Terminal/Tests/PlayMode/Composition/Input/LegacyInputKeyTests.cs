using NUnit.Framework;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Tests.PlayMode.Composition.Input
{
    [TestFixture]
    public sealed class LegacyInputKeyTests
    {
        private LegacyInputKey _key;

        [SetUp]
        public void SetUp()
        {
            _key = new LegacyInputKey();
        }

        /// <summary>Cancelアクションの既定キーがC、既定修飾キーがLeftControl単体であることを検証する.</summary>
        [Test]
        public void GetKeyAndModifiers_Cancel_DefaultsToCtrlC()
        {
            Assert.AreEqual(KeyCode.C, _key.GetKey(TerminalAction.Cancel));

            var modifiers = _key.GetModifiers(TerminalAction.Cancel);
            Assert.AreEqual(1, modifiers.Count);
            Assert.AreEqual(KeyCode.LeftControl, modifiers[0]);
        }

        /// <summary>Cancel以外のアクションは既定で修飾キーを持たない(空)ことを検証する.</summary>
        [TestCase(TerminalAction.Open)]
        [TestCase(TerminalAction.Close)]
        [TestCase(TerminalAction.Execute)]
        [TestCase(TerminalAction.PreviousHistory)]
        [TestCase(TerminalAction.NextHistory)]
        [TestCase(TerminalAction.Autocomplete)]
        [TestCase(TerminalAction.Focus)]
        public void GetModifiers_OtherActions_DefaultsToEmpty(TerminalAction action)
        {
            Assert.IsEmpty(_key.GetModifiers(action));
        }
    }
}

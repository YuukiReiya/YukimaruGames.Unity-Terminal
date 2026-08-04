using System;
using NUnit.Framework;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Composition.Input.InputSystem;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Tests.PlayMode.Composition.Input
{
    /// <summary>
    /// <see cref="InputSystemKey"/>の既定キー設定と異常入力に対する契約を検証する.
    /// </summary>
    [TestFixture]
    public sealed class InputSystemKeyTests
    {
        private InputSystemKey _key;

        /// <summary>各テスト実行前に既定設定の<see cref="InputSystemKey"/>を生成する.</summary>
        [SetUp]
        public void SetUp()
        {
            _key = new InputSystemKey();
        }

        /// <summary>Cancelアクションの既定キーがC、既定修飾キーがLeftCtrl単体であることを検証する.</summary>
        [Test]
        public void GetKeyAndModifiers_Cancel_DefaultsToCtrlC()
        {
            Assert.AreEqual(Key.C, _key.GetKey(TerminalAction.Cancel));

            var modifiers = _key.GetModifiers(TerminalAction.Cancel);
            Assert.AreEqual(1, modifiers.Count);
            Assert.AreEqual(Key.LeftCtrl, modifiers[0]);
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

        /// <summary>未定義の<see cref="TerminalAction"/>を渡すと<see cref="GetKey"/>が拒否することを検証する.</summary>
        [Test]
        public void GetKey_UndefinedAction_ThrowsArgumentOutOfRangeException()
        {
            var undefined = (TerminalAction)(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => _key.GetKey(undefined));
        }

        /// <summary>未定義の<see cref="TerminalAction"/>を渡すと<see cref="GetModifiers"/>が拒否することを検証する.</summary>
        [Test]
        public void GetModifiers_UndefinedAction_ThrowsArgumentOutOfRangeException()
        {
            var undefined = (TerminalAction)(-1);
            Assert.Throws<ArgumentOutOfRangeException>(() => _key.GetModifiers(undefined));
        }
    }
}

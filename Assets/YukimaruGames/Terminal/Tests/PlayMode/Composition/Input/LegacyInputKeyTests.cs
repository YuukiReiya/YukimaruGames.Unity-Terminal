// 検証対象のLegacyInputKeyは、Legacy Input Managerが有効なときにだけ存在する
// (Player SettingsのActive Input HandlingがInput System Package (New)のみだと、
// 実装ファイル側が#if ENABLE_LEGACY_INPUT_MANAGERで丸ごと除外される)。
// このガードが無いと、New専用のプロジェクトへ本パッケージを入れた時点で
// テストアセンブリがコンパイルエラーになる(#162).
#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using NUnit.Framework;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Tests.PlayMode.Composition.Input
{
    /// <summary>
    /// <see cref="LegacyInputKey"/>の既定キー設定と異常入力に対する契約を検証する.
    /// </summary>
    [TestFixture]
    public sealed class LegacyInputKeyTests
    {
        private LegacyInputKey _key;

        /// <summary>各テスト実行前に既定設定の<see cref="LegacyInputKey"/>を生成する.</summary>
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
#endif

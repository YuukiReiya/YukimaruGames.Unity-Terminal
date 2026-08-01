using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using YukimaruGames.Terminal.Adapters.Input.InputSystem;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.Input.InputSystem
{
    /// <summary>
    /// <see cref="InputSystemAdapter"/>のIME変換状態通知を検証する.
    /// </summary>
    [TestFixture]
    public sealed class InputSystemAdapterTests
    {
        private GameObject _gameObject;
        private InputSystemAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(InputSystemAdapterTests));
            _adapter = _gameObject.AddComponent<InputSystemAdapter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.Destroy(_gameObject);
            }
        }

        /// <summary>変換文字列が非空になるとIME変換中(true)が通知されることを検証する.</summary>
        [UnityTest]
        public IEnumerator HandleCompositionChanged_NonEmptyComposition_NotifiesComposingTrue()
        {
            yield return null;

            _adapter.SetFocus(true);

            var notified = false;
            var lastValue = false;
            _adapter.OnImeComposingStateChanged += value =>
            {
                notified = true;
                lastValue = value;
            };

            _adapter.HandleCompositionChanged(new IMECompositionString("あ"));

            Assert.IsTrue(notified);
            Assert.IsTrue(lastValue);
        }

        /// <summary>変換文字列が空に戻るとIME変換中(false)が通知されることを検証する.</summary>
        [UnityTest]
        public IEnumerator HandleCompositionChanged_EmptyComposition_NotifiesComposingFalse()
        {
            yield return null;

            _adapter.SetFocus(true);
            _adapter.HandleCompositionChanged(new IMECompositionString("あ"));

            var notified = false;
            var lastValue = true;
            _adapter.OnImeComposingStateChanged += value =>
            {
                notified = true;
                lastValue = value;
            };

            _adapter.HandleCompositionChanged(new IMECompositionString(string.Empty));

            Assert.IsTrue(notified);
            Assert.IsFalse(lastValue);
        }

        /// <summary>同一の変換状態が続く場合は重複通知しないことを検証する.</summary>
        [UnityTest]
        public IEnumerator HandleCompositionChanged_SameState_DoesNotNotifyAgain()
        {
            yield return null;

            _adapter.SetFocus(true);
            _adapter.HandleCompositionChanged(new IMECompositionString("あ"));

            var notifyCount = 0;
            _adapter.OnImeComposingStateChanged += _ => notifyCount++;

            _adapter.HandleCompositionChanged(new IMECompositionString("あい"));

            Assert.AreEqual(0, notifyCount);
        }

        /// <summary>変換中にフォーカスを外すとIME変換中がfalseへ強制的にリセットされることを検証する.</summary>
        [UnityTest]
        public IEnumerator SetFocus_False_WhileComposing_ResetsComposingState()
        {
            yield return null;

            _adapter.SetFocus(true);
            _adapter.HandleCompositionChanged(new IMECompositionString("あ"));

            var notified = false;
            var lastValue = true;
            _adapter.OnImeComposingStateChanged += value =>
            {
                notified = true;
                lastValue = value;
            };

            _adapter.SetFocus(false);

            Assert.IsTrue(notified);
            Assert.IsFalse(lastValue);
        }

        /// <summary>フォーカス解除後にIME変換コールバックが来ても無視されることを検証する.</summary>
        [UnityTest]
        public IEnumerator HandleCompositionChanged_AfterFocusLost_IsIgnored()
        {
            yield return null;

            _adapter.SetFocus(true);
            _adapter.SetFocus(false);

            var notified = false;
            _adapter.OnImeComposingStateChanged += _ => notified = true;

            _adapter.HandleCompositionChanged(new IMECompositionString("あ"));

            Assert.IsFalse(notified);
        }
    }
}

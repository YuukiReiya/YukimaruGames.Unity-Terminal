using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YukimaruGames.Terminal.Adapters.Input;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.Input.LegacyInput
{
    /// <summary>
    /// <see cref="LegacyInputAdapter"/>の入力テキスト更新通知を検証する.
    /// </summary>
    [TestFixture]
    public sealed class LegacyInputAdapterTests
    {
        private GameObject _gameObject;
        private LegacyInputAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(LegacyInputAdapterTests));
            _adapter = _gameObject.AddComponent<LegacyInputAdapter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.Destroy(_gameObject);
            }
        }

        /// <summary>異なる値を設定するとOnInputTextChangedが発火することを検証する.</summary>
        [UnityTest]
        public IEnumerator SetInputText_DifferentValue_NotifiesChanged()
        {
            yield return null;

            var notified = false;
            var lastValue = string.Empty;
            _adapter.OnInputTextChanged += value =>
            {
                notified = true;
                lastValue = value;
            };

            _adapter.SetInputText("hello");

            Assert.IsTrue(notified);
            Assert.AreEqual("hello", lastValue);
        }

        /// <summary>同一の値を設定した場合は重複通知しないことを検証する.</summary>
        [UnityTest]
        public IEnumerator SetInputText_SameValue_DoesNotNotifyAgain()
        {
            yield return null;

            _adapter.SetInputText("hello");

            var notifyCount = 0;
            _adapter.OnInputTextChanged += _ => notifyCount++;

            _adapter.SetInputText("hello");

            Assert.AreEqual(0, notifyCount);
        }

        /// <summary>nullを設定した場合は空文字として通知されることを検証する.</summary>
        [UnityTest]
        public IEnumerator SetInputText_Null_NotifiesEmptyString()
        {
            yield return null;

            _adapter.SetInputText("hello");

            var lastValue = "unset";
            _adapter.OnInputTextChanged += value => lastValue = value;

            _adapter.SetInputText(null);

            Assert.AreEqual(string.Empty, lastValue);
        }
    }
}

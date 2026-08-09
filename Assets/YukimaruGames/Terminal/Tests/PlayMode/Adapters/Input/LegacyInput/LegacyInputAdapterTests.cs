using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YukimaruGames.Terminal.Adapters.Input;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.Input.LegacyInput
{
    /// <summary>
    /// <see cref="LegacyInputAdapter"/>の入力テキスト更新通知・書記素クラスタ単位の削除を検証する.
    /// </summary>
    [TestFixture]
    public sealed class LegacyInputAdapterTests
    {
        private GameObject _gameObject;
        private LegacyInputAdapter _adapter;

        /// <summary>検証対象の<see cref="LegacyInputAdapter"/>を生成する.</summary>
        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject(nameof(LegacyInputAdapterTests));
            _adapter = _gameObject.AddComponent<LegacyInputAdapter>();
        }

        /// <summary>検証対象のGameObjectを破棄する.</summary>
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

        /// <summary>サロゲートペア（絵文字等）の末尾1文字が書記素クラスタ単位で削除されることを検証する.</summary>
        [Test]
        public void RemoveLastTextElement_SurrogatePair_RemovesWholeCharacter()
        {
            const string Text = "abc\U0001F600"; // "abc" + 😀 (surrogate pair)

            var result = LegacyInputAdapter.RemoveLastTextElement(Text);

            Assert.AreEqual("abc", result);
        }

        /// <summary>結合文字を含む末尾1文字が書記素クラスタ単位で削除されることを検証する.</summary>
        [Test]
        public void RemoveLastTextElement_CombiningCharacter_RemovesWholeCluster()
        {
            const string Text = "abce\u0301"; // "abc" + "e" + combining acute accent (U+0301)

            var result = LegacyInputAdapter.RemoveLastTextElement(Text);

            Assert.AreEqual("abc", result);
        }

        /// <summary>末尾がASCII文字1文字の場合はその1文字のみ削除されることを検証する.</summary>
        [Test]
        public void RemoveLastTextElement_AsciiCharacter_RemovesSingleCharacter()
        {
            const string Text = "abc";

            var result = LegacyInputAdapter.RemoveLastTextElement(Text);

            Assert.AreEqual("ab", result);
        }
    }
}

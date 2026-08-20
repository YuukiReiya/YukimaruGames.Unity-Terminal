using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.UIToolkit
{
    /// <summary>
    /// テストから<see cref="VisualElement"/>を操作するための基本パターンを検証する.
    /// </summary>
    /// <remarks>
    /// UIToolkitはIMGUIやuGUIと操作方法が異なり、テストからの疑似入力の書き方が定まっていなかった(#127)。
    /// ここで確立した手順（クリック・テキスト入力・フォーカス）を、以降のUIToolkit向けテストの
    /// 土台として使う。手順が壊れると後続のテストが一斉に無意味になるため、パターン自体を固定する.
    /// </remarks>
    [TestFixture]
    public sealed class VisualElementInteractionTests
    {
        private const float ButtonSize = 50f;

        private UIToolkitTestPanel _panel;

        /// <summary>各テスト前にパネルを用意する.</summary>
        [SetUp]
        public void SetUp() => _panel = new UIToolkitTestPanel();

        /// <summary>各テスト後にパネルを破棄する.</summary>
        [TearDown]
        public void TearDown() => _panel?.Dispose();

        /// <summary>
        /// ボタンのクリックを疑似入力で発火できることを検証する.
        /// </summary>
        /// <remarks>
        /// <see cref="Button"/>の反応は<c>Clickable</c>マニピュレータが担っており、
        /// ポインタの押下と解放の<b>両方</b>を送る必要がある。<c>ClickEvent</c>単体では発火しない.
        /// </remarks>
        [UnityTest]
        public IEnumerator ボタンのクリックを疑似入力で発火できる()
        {
            var clicked = 0;
            var button = new Button(() => clicked++) { style = { width = ButtonSize, height = ButtonSize } };
            _panel.Root.Add(button);

            // レイアウトが解決してからでないと、要素の位置が確定せずヒットテストが安定しない.
            yield return null;

            Click(button);
            yield return null;

            Assert.That(clicked, Is.EqualTo(1));
        }

        /// <summary>
        /// テキスト入力が値の変更通知として届くことを検証する.
        /// </summary>
        /// <remarks>
        /// キーストロークを1文字ずつ送る必要はなく、<see cref="TextField.value"/>への代入で
        /// <see cref="ChangeEvent{T}"/>が発火する（利用者の入力と同じ扱いになる）.
        /// </remarks>
        [UnityTest]
        public IEnumerator テキスト入力が値の変更通知として届く()
        {
            const string typed = "commands";

            string received = null;
            var field = new TextField();
            field.RegisterValueChangedCallback(e => received = e.newValue);
            _panel.Root.Add(field);
            yield return null;

            field.value = typed;
            yield return null;

            Assert.That(received, Is.EqualTo(typed));
            Assert.That(field.value, Is.EqualTo(typed));
        }

        /// <summary>
        /// フォーカスの移動を検証する.
        /// </summary>
        /// <remarks>
        /// フォーカスはパネル単位で管理されるため、<see cref="IPanel.focusController"/>から確認する。
        /// <see cref="VisualElement.Focus"/>は<c>focusable</c>な要素にしか効かない.
        /// </remarks>
        [UnityTest]
        public IEnumerator フォーカスの移動をパネル経由で確認できる()
        {
            var field = new TextField();
            _panel.Root.Add(field);
            yield return null;

            field.Focus();
            yield return null;

            Assert.That(field.panel, Is.Not.Null, "前提: 要素がパネルへ接続されていること");
            Assert.That(field.panel.focusController.focusedElement, Is.SameAs(field));

            field.Blur();
            yield return null;

            Assert.That(field.panel.focusController.focusedElement, Is.Not.SameAs(field));
        }

        /// <summary>
        /// ポインタの押下と解放を送ってクリックを再現する.
        /// </summary>
        /// <remarks>
        /// 座標は要素の中心。<see cref="EventBase{T}.target"/>を明示することで、
        /// レイアウトの解決状況に依存せず目的の要素へ届く.
        /// </remarks>
        private static void Click(VisualElement element)
        {
            var center = element.worldBound.center;

            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = center, button = 0 }))
            {
                down.target = element;
                element.SendEvent(down);
            }

            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = center, button = 0 }))
            {
                up.target = element;
                element.SendEvent(up);
            }
        }
    }
}

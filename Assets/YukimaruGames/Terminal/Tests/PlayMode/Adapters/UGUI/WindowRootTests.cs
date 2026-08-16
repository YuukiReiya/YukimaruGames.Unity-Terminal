using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.UGUI;
using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.UGUI
{
    /// <summary>
    /// <see cref="WindowRoot.ApplyRect"/>による<see cref="TerminalRect"/>から
    /// <see cref="RectTransform"/>への変換を検証する.
    /// </summary>
    /// <remarks>
    /// <see cref="TerminalRect"/>は左上原点・Y下向き、<see cref="RectTransform"/>はY上向きのため、
    /// アンカーとpivotを左上へ固定したうえでY成分の符号を反転する必要がある。
    /// ここが崩れるとウィンドウが画面外へ飛ぶ、上下が反転する等の形で表面化する.
    /// </remarks>
    [TestFixture]
    public sealed class WindowRootTests
    {
        private const float CanvasWidth = 1920f;
        private const float CanvasHeight = 1080f;

        private GameObject _canvasGameObject;
        private WindowRoot _windowRoot;

        [SetUp]
        public void SetUp()
        {
            // 画面サイズに依存しないよう、Overlayではなく固定サイズのCanvasを組む.
            _canvasGameObject = new GameObject("Test Canvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = _canvasGameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = (RectTransform)_canvasGameObject.transform;
            canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

            _windowRoot = _canvasGameObject.AddComponent<WindowRoot>();
            _windowRoot.Initialize(canvasRect);
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasGameObject != null) Object.DestroyImmediate(_canvasGameObject);
        }

        [UnityTest]
        public IEnumerator ApplyRect_アンカーとpivotが左上へ固定される()
        {
            _windowRoot.ApplyRect(new TerminalRect(0f, 0f, 100f, 50f));
            yield return null;

            Assert.That(_windowRoot.Root.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(_windowRoot.Root.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(_windowRoot.Root.pivot, Is.EqualTo(new Vector2(0f, 1f)));
        }

        [UnityTest]
        public IEnumerator ApplyRect_サイズがそのまま反映される()
        {
            _windowRoot.ApplyRect(new TerminalRect(0f, 0f, 640f, 360f));
            yield return null;

            Assert.That(_windowRoot.Root.sizeDelta, Is.EqualTo(new Vector2(640f, 360f)));
        }

        [UnityTest]
        public IEnumerator ApplyRect_Y成分の符号が反転する()
        {
            _windowRoot.ApplyRect(new TerminalRect(120f, 300f, 640f, 360f));
            yield return null;

            // 左上原点で下方向へ300 → RectTransformでは-300.
            Assert.That(_windowRoot.Root.anchoredPosition, Is.EqualTo(new Vector2(120f, -300f)));
        }

        /// <summary>
        /// <see cref="WindowAnchor"/>4種 × <see cref="WindowStyle"/>2種に相当する矩形を与え、
        /// ウィンドウがCanvas内の意図した側へ配置されることを確認する.
        /// </summary>
        /// <remarks>
        /// 矩形の算出自体はPresentation層(<c>WindowAnimator</c>)の責務のため、ここでは各組み合わせで
        /// 生成される代表的な矩形を直接与え、uGUI側の座標変換が破綻しないことだけを見る.
        /// </remarks>
        [UnityTest]
        public IEnumerator ApplyRect_アンカーとスタイルの組み合わせで意図した側へ配置される(
            [Values(WindowAnchor.Left, WindowAnchor.Right, WindowAnchor.Top, WindowAnchor.Bottom)] WindowAnchor anchor,
            [Values(WindowStyle.Compact, WindowStyle.Full)] WindowStyle style)
        {
            var ratio = style == WindowStyle.Full ? 1f : 0.5f;
            var rect = CreateRect(anchor, ratio);

            _windowRoot.ApplyRect(rect);
            yield return null;

            var position = _windowRoot.Root.anchoredPosition;
            var size = _windowRoot.Root.sizeDelta;

            // 左上原点の矩形を、左上固定のRectTransformへ写したときの各辺(Y下向き)。
            var left = position.x;
            var top = -position.y;
            var right = left + size.x;
            var bottom = top + size.y;

            Assert.That(left, Is.GreaterThanOrEqualTo(0f), "左辺がCanvasの外へ出ている");
            Assert.That(top, Is.GreaterThanOrEqualTo(0f), "上辺がCanvasの外へ出ている");
            Assert.That(right, Is.LessThanOrEqualTo(CanvasWidth + Mathf.Epsilon), "右辺がCanvasの外へ出ている");
            Assert.That(bottom, Is.LessThanOrEqualTo(CanvasHeight + Mathf.Epsilon), "下辺がCanvasの外へ出ている");

            switch (anchor)
            {
                case WindowAnchor.Left:
                    Assert.That(left, Is.EqualTo(0f), "左寄せなのに左辺が0でない");
                    break;
                case WindowAnchor.Right:
                    Assert.That(right, Is.EqualTo(CanvasWidth), "右寄せなのに右辺がCanvas幅と一致しない");
                    break;
                case WindowAnchor.Top:
                    Assert.That(top, Is.EqualTo(0f), "上寄せなのに上辺が0でない");
                    break;
                case WindowAnchor.Bottom:
                    Assert.That(bottom, Is.EqualTo(CanvasHeight), "下寄せなのに下辺がCanvas高さと一致しない");
                    break;
            }
        }

        /// <summary>
        /// 指定アンカーへ寄せた、画面に対して<paramref name="ratio"/>の大きさを持つ矩形を作る.
        /// </summary>
        private static TerminalRect CreateRect(WindowAnchor anchor, float ratio) => anchor switch
        {
            WindowAnchor.Left => new TerminalRect(0f, 0f, CanvasWidth * ratio, CanvasHeight),
            WindowAnchor.Right => new TerminalRect(CanvasWidth * (1f - ratio), 0f, CanvasWidth * ratio, CanvasHeight),
            WindowAnchor.Top => new TerminalRect(0f, 0f, CanvasWidth, CanvasHeight * ratio),
            WindowAnchor.Bottom => new TerminalRect(0f, CanvasHeight * (1f - ratio), CanvasWidth, CanvasHeight * ratio),
            _ => default,
        };
    }
}

#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UI;
using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Launcher;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.UGUI.Renderers
{
    /// <summary>
    /// ランチャーボタン(開く/閉じる)の描画と配置を行う.
    /// </summary>
    /// <remarks>
    /// <see cref="_container"/>をウィンドウのRectに対して<see cref="WindowAnchor"/>で示される
    /// 外側へ配置する。座標の対応はUIToolkit版と揃えてある.
    /// </remarks>
    public sealed class LauncherRenderer : ILauncherRenderer, IDisposable
    {
        private const string CompactButtonText = "[-]";
        private const string CloseButtonText = "[x]";

        private readonly RectTransform _container;
        private readonly Button _openButton;
        private readonly Button _closeButton;

        /// <inheritdoc/>
        public event Action OnClickOpenButton;
        /// <inheritdoc/>
        public event Action OnClickCloseButton;

        public LauncherRenderer(RectTransform container, Button openButton, Button closeButton)
        {
            _container = container;
            _openButton = openButton;
            _closeButton = closeButton;

            if (_container != null)
            {
                // 左上原点(TerminalRect)との対応を取るため、アンカーとpivotを左上に固定する.
                _container.anchorMin = new Vector2(0f, 1f);
                _container.anchorMax = new Vector2(0f, 1f);
                _container.pivot = new Vector2(0f, 1f);

            }

            SetButtonText(_openButton, CompactButtonText);
            SetButtonText(_closeButton, CloseButtonText);

            if (_openButton != null) _openButton.onClick.AddListener(HandleOpenClicked);
            if (_closeButton != null) _closeButton.onClick.AddListener(HandleCloseClicked);
        }

        /// <inheritdoc/>
        public void Render(LauncherRenderData renderData)
        {
            if (_container == null) return;

            if (_container.gameObject.activeSelf != renderData.IsVisible)
            {
                _container.gameObject.SetActive(renderData.IsVisible);
            }

            if (!renderData.IsVisible) return;

            ApplyButtonPlacement(renderData.Anchor);
            ApplyPosition(renderData.Anchor, renderData.WindowRect, renderData.IsReverse);
        }

        /// <summary>
        /// 2つのボタンをアンカーに応じて縦または横へ並べる.
        /// </summary>
        /// <remarks>
        /// uGUIの<see cref="HorizontalLayoutGroup"/>と<see cref="VerticalLayoutGroup"/>は
        /// 同一GameObjectに共存できず(どちらも<c>HorizontalOrVerticalLayoutGroup</c>で、
        /// Unityが後勝ちの追加を拒否する)、実行時に型を差し替えることもできない。
        /// 要素が2つだけなので、LayoutGroupに頼らずここで直接配置する.
        /// </remarks>
        private void ApplyButtonPlacement(WindowAnchor anchor)
        {
            var vertical = anchor is WindowAnchor.Left or WindowAnchor.Right;

            var openRect = _openButton != null ? _openButton.GetComponent<RectTransform>() : null;
            var closeRect = _closeButton != null ? _closeButton.GetComponent<RectTransform>() : null;
            if (openRect == null || closeRect == null) return;

            openRect.anchoredPosition = Vector2.zero;
            closeRect.anchoredPosition = vertical
                ? new Vector2(0f, -openRect.sizeDelta.y)
                : new Vector2(openRect.sizeDelta.x, 0f);
        }

        /// <summary>
        /// ウィンドウ矩形の外側へコンテナを配置する.
        /// </summary>
        /// <remarks>
        /// <see cref="TerminalRect"/>は左上原点・Y下向き、<see cref="RectTransform"/>はY上向きの
        /// ため、Y成分の符号を反転して<c>anchoredPosition</c>へ渡す.
        /// </remarks>
        private void ApplyPosition(WindowAnchor anchor, TerminalRect rect, bool isReverse)
        {
            float x;
            float y;

            switch (anchor)
            {
                case WindowAnchor.Left:
                    x = rect.X + rect.Width;
                    y = isReverse ? rect.Y + rect.Height : rect.Y;
                    break;
                case WindowAnchor.Right:
                    x = rect.X;
                    y = isReverse ? rect.Y + rect.Height : rect.Y;
                    break;
                case WindowAnchor.Top:
                    x = isReverse ? rect.X + rect.Width : rect.X;
                    y = rect.Y + rect.Height;
                    break;
                case WindowAnchor.Bottom:
                    x = isReverse ? rect.X + rect.Width : rect.X;
                    y = rect.Y;
                    break;
                default:
                    return;
            }

            _container.anchoredPosition = new Vector2(x, -y);
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null) return;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = text;
        }

        private void HandleOpenClicked() => OnClickOpenButton?.Invoke();

        private void HandleCloseClicked() => OnClickCloseButton?.Invoke();

        void IDisposable.Dispose()
        {
            if (_openButton != null) _openButton.onClick.RemoveListener(HandleOpenClicked);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(HandleCloseClicked);

            OnClickOpenButton = null;
            OnClickCloseButton = null;
        }
    }
}
#endif

#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Launcher;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.UIToolkit.Renderers
{
    /// <summary>
    /// UIToolkitによる開閉ランチャーボタンの描画実装.
    /// </summary>
    /// <remarks>
    /// <see cref="_container"/>をウィンドウのRectに対して<see cref="WindowAnchor"/>で示される外側へ配置する.
    /// IMGUI版はボタンテキストの実測サイズでオフセットを計算しているが、UIToolkitはレイアウト計算後の
    /// 実サイズを描画前に取得できないため、代わりに<c>translate: -100%</c>を用いて要素自身のサイズを基準に
    /// 端を合わせる手法を採る.
    /// </remarks>
    public sealed class LauncherRenderer : ILauncherRenderer, IDisposable
    {
        private const string CompactButtonText = "[-]";
        private const string CloseButtonText = "[x]";

        private readonly VisualElement _container;
        private readonly Button _openButton;
        private readonly Button _closeButton;

        public event Action OnClickOpenButton;
        public event Action OnClickCloseButton;

        public LauncherRenderer(VisualElement container, Button openButton, Button closeButton)
        {
            _container = container;
            _openButton = openButton;
            _closeButton = closeButton;

            if (_container != null)
            {
                _container.style.position = Position.Absolute;
            }

            if (_openButton != null)
            {
                _openButton.text = CompactButtonText;
                _openButton.clicked += HandleOpenClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.text = CloseButtonText;
                _closeButton.clicked += HandleCloseClicked;
            }
        }

        public void Render(LauncherRenderData renderData)
        {
            if (_container == null) return;

            if (!renderData.IsVisible)
            {
                _container.style.display = DisplayStyle.None;
                return;
            }

            _container.style.display = DisplayStyle.Flex;

            var anchor = renderData.Anchor;
            var rect = renderData.WindowRect;
            _container.style.flexDirection = anchor is WindowAnchor.Left or WindowAnchor.Right
                ? FlexDirection.Column
                : FlexDirection.Row;

            ApplyPosition(anchor, rect, renderData.IsReverse);
        }

        private void ApplyPosition(WindowAnchor anchor, YukimaruGames.Terminal.Domain.Models.TerminalRect rect, bool isReverse)
        {
            var translateX = 0f;
            var translateY = 0f;

            switch (anchor)
            {
                case WindowAnchor.Left:
                    _container.style.left = rect.X + rect.Width;
                    if (isReverse)
                    {
                        _container.style.top = rect.Y + rect.Height;
                        translateY = -100f;
                    }
                    else
                    {
                        _container.style.top = rect.Y;
                    }

                    break;

                case WindowAnchor.Right:
                    _container.style.left = rect.X;
                    translateX = -100f;
                    if (!isReverse)
                    {
                        _container.style.top = rect.Y + rect.Height;
                        translateY = -100f;
                    }
                    else
                    {
                        _container.style.top = rect.Y;
                    }

                    break;

                case WindowAnchor.Top:
                    _container.style.top = rect.Y + rect.Height;
                    if (isReverse)
                    {
                        _container.style.left = rect.X + rect.Width;
                        translateX = -100f;
                    }
                    else
                    {
                        _container.style.left = rect.X;
                    }

                    break;

                case WindowAnchor.Bottom:
                    _container.style.top = rect.Y;
                    translateY = -100f;
                    if (!isReverse)
                    {
                        _container.style.left = rect.X + rect.Width;
                        translateX = -100f;
                    }
                    else
                    {
                        _container.style.left = rect.X;
                    }

                    break;
            }

            _container.style.translate = new Translate(
                new Length(translateX, LengthUnit.Percent),
                new Length(translateY, LengthUnit.Percent));
        }

        private void HandleOpenClicked() => OnClickOpenButton?.Invoke();

        private void HandleCloseClicked() => OnClickCloseButton?.Invoke();

        void IDisposable.Dispose()
        {
            if (_openButton != null) _openButton.clicked -= HandleOpenClicked;
            if (_closeButton != null) _closeButton.clicked -= HandleCloseClicked;

            OnClickOpenButton = null;
            OnClickCloseButton = null;
        }
    }
}
#endif

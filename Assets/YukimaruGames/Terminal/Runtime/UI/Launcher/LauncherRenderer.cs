using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Core;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.UI.Launcher
{
    public sealed class LauncherRenderer : ILauncherRenderer, IDisposable
    {
        private readonly IGUIStyleProvider _styleProvider;

        private Vector2 _openButtonTextSize;
        private Vector2 _closeButtonTextSize;

        /// <summary>
        /// 再計算するか
        /// </summary>
        /// <remarks>
        /// <p>初期値を true に設定することで初回描画時に遅延実行を促し適切なサイズを計算させる</p>
        /// <p>※コンストラクタで初期化すると正しいScreen.Sizeが入っておらず意図したサイズに計算されないため</p>
        /// </remarks>
        private bool _shouldRecalculation = true;

        /// <remarks>
        /// Windowと同じKeyを指定することでカラーを同期する.
        /// </remarks>
        private const string Key = "WindowGUIStyle";

        private const string CompactButtonText = "[-]";
        private const string CloseButtonText = "[x]";

        public event Action OnClickOpenButton;
        public event Action OnClickCloseButton;

        public LauncherRenderer(IPixelTextureRepository repository, IGUIStyleProvider styleProvider)
        {
            _styleProvider = styleProvider;

            _styleProvider.OnStyleChanged += OnStyleChanged;
            _styleProvider.GetStyle().normal.background = repository.GetTexture2D(Key);
        }

        public void Render(LauncherRenderData renderData)
        {
            if (!renderData.IsVisible) return;

            if (_shouldRecalculation)
            {
                Recalculate();
                _shouldRecalculation = false;
            }

            var anchor = renderData.Anchor;
            var rect = renderData.WindowRect;

            switch (anchor)
            {
                // 左上(左下)
                case WindowAnchor.Left:
                    rect.x += rect.width;
                    rect.width = Mathf.Ceil(Mathf.Max(_openButtonTextSize.x, _closeButtonTextSize.x));
                    var height = Mathf.Floor(_openButtonTextSize.y + _closeButtonTextSize.y);
                    if (renderData.IsReverse) rect.y = rect.height - height;
                    rect.height = height;
                    break;

                // 右下(右上)
                case WindowAnchor.Right:
                    var width = Mathf.Ceil(Mathf.Max(_openButtonTextSize.x, _closeButtonTextSize.x));
                    rect.x -= width;
                    rect.width = width;
                    height = Mathf.Floor(_openButtonTextSize.y + _closeButtonTextSize.y);
                    if (!renderData.IsReverse) rect.y = rect.height - height;
                    rect.height = height;
                    break;

                // 左上(右上)
                case WindowAnchor.Top:
                    width = Mathf.Ceil(_openButtonTextSize.x + _closeButtonTextSize.x);
                    height = Mathf.Max(_openButtonTextSize.y, _closeButtonTextSize.y);
                    if (renderData.IsReverse) rect.x = rect.width - width;
                    rect.y += rect.height;
                    rect.width = width;
                    rect.height = height;
                    break;

                // 右下(左下)
                case WindowAnchor.Bottom:
                    width = Mathf.Ceil(_openButtonTextSize.x + _closeButtonTextSize.x);
                    height = Mathf.Max(_openButtonTextSize.y, _closeButtonTextSize.y);
                    if (!renderData.IsReverse) rect.x = rect.width - width;
                    rect.y -= height;
                    rect.width = width;
                    rect.height = height;
                    break;
            }

            using (new GUILayout.AreaScope(rect))
            {
                using (GUI.Scope _ = anchor is WindowAnchor.Left or WindowAnchor.Right ? new GUILayout.VerticalScope() : new GUILayout.HorizontalScope())
                {
                    DrawButtons();
                }
            }
        }

        private void Recalculate()
        {
            _openButtonTextSize = CalcCompactButtonTextSize();
            _closeButtonTextSize = CalcFullButtonTextSize();
        }

        private void DrawButtons()
        {
            if (GUILayout.Button(CompactButtonText, _styleProvider.GetStyle()))
            {
                OnClickOpenButton?.Invoke();
            }
            else if (GUILayout.Button(CloseButtonText, _styleProvider.GetStyle()))
            {
                OnClickCloseButton?.Invoke();
            }
        }

        private Vector2 CalcCompactButtonTextSize() =>
            _styleProvider.GetStyle().CalcSize(new GUIContent(CompactButtonText));

        private Vector2 CalcFullButtonTextSize() =>
            _styleProvider.GetStyle().CalcSize(new GUIContent(CloseButtonText));

        private void OnStyleChanged()
        {
            _shouldRecalculation = true;
        }

        void IDisposable.Dispose()
        {
            _styleProvider.OnStyleChanged -= OnStyleChanged;

            OnClickOpenButton = null;
            OnClickCloseButton = null;
        }
    }
}

using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalCloseButtonRenderer : ITerminalCloseButtonRenderer, IDisposable
    {
        private readonly IGUIStyleProvider _provider;

        private bool _shouldRecalculation = true;
        private Vector2 _buttonTextSize;

        private const string Key = "WindowGUIStyle";
        private const string ButtonText = "[x]";

        public event Action OnClickButton;

        public TerminalCloseButtonRenderer(IPixelTexture2DRepository repository, IGUIStyleProvider provider)
        {
            _provider = provider;

            _provider.OnStyleChanged += OnStyleChanged;
            _provider.GetStyle().normal.background = repository.GetTexture2D(Key);
        }

        public void Render(TerminalCloseButtonRenderData renderData)
        {
            if (!renderData.IsVisible) return;

            if (_shouldRecalculation)
            {
                Recalculate();
                _shouldRecalculation = false;
            }

            var rect = renderData.WindowRect;
            var anchor = renderData.Anchor;
            
            switch (anchor)
            {
                // 左上(左下)
                case TerminalAnchor.Left:
                    rect.x = rect.width;
                    rect.width = _buttonTextSize.x;
                    var height = _buttonTextSize.y;
                    if (renderData.IsReverse) rect.y = rect.height - height;
                    rect.height = height;
                    break;
                
                // 右下(右上)
                case TerminalAnchor.Right:
                    var width = _buttonTextSize.x;
                    rect.x -= width;
                    rect.width = width;
                    height = _buttonTextSize.y;
                    if (!renderData.IsReverse) rect.y = rect.height - height;
                    rect.height = height;
                    break;
                
                // 左上(右上)
                case TerminalAnchor.Top:
                    width = _buttonTextSize.x;
                    height = _buttonTextSize.y;
                    if (renderData.IsReverse) rect.x = rect.width - width;
                    rect.y = rect.height;
                    rect.width = width;
                    rect.height = height;
                    break;
                
                // 右下(左下)
                case TerminalAnchor.Bottom:
                    width = _buttonTextSize.x;
                    height = _buttonTextSize.y;
                    if (!renderData.IsReverse) rect.x = rect.width - width;
                    rect.y -= height;
                    rect.width = width;
                    rect.height = height;
                    break;
            }

            using (new GUILayout.AreaScope(rect))
            {
                using (GUI.Scope _ = anchor is TerminalAnchor.Left or TerminalAnchor.Right ?
                       new GUILayout.VerticalScope() :
                       new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(ButtonText, _provider.GetStyle()))
                    {
                        OnClickButton?.Invoke();
                    }
                }
            }
        }

        private void OnStyleChanged()
        {
            _shouldRecalculation = true;
        }

        private void Recalculate()
        {
            _buttonTextSize = _provider.GetStyle().CalcSize(new GUIContent(ButtonText));
            _buttonTextSize.x = Mathf.Ceil(_buttonTextSize.x);
            _buttonTextSize.y = Mathf.Ceil(_buttonTextSize.x);
        }

        public void Dispose()
        {
            _provider.OnStyleChanged -= OnStyleChanged;

            OnClickButton = null;
        }
    }
}

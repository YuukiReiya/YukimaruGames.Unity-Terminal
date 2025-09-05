using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View.Model;
using Action = Unity.Plastic.Newtonsoft.Json.Serialization.Action;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalOpenButtonRenderer : ITerminalOpenButtonRenderer,IDisposable
    {
        private readonly IGUIStyleProvider _styleProvider;

        private Vector2 _compactButtonTextSize;
        private Vector2 _fullButtonTextSize;
        
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
        private const string FullButtonText = "[□]";
        
        public event Action OnClickCompactOpenButton;
        public event Action OnClickFullOpenButton;

        public TerminalOpenButtonRenderer(IPixelTexture2DRepository repository, IGUIStyleProvider styleProvider)
        {
            _styleProvider = styleProvider;

            _styleProvider.OnStyleChanged += OnStyleChanged;
            _styleProvider.GetStyle().normal.background = repository.GetTexture2D(Key);
        }
        
        public void Render(TerminalOpenButtonRenderData renderData)
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
                case TerminalAnchor.Left:
                    rect.x = rect.width;
                    rect.width = Mathf.Ceil(Mathf.Max(_compactButtonTextSize.x, _fullButtonTextSize.x));
                    var height = _compactButtonTextSize.y + _fullButtonTextSize.y;
                    if (renderData.IsReverse) rect.y = rect.height - height;
                    rect.height = height;
                    break;
                
                // 右下(右上)
                case TerminalAnchor.Right:
                    var width = Mathf.Ceil(Mathf.Max(_compactButtonTextSize.x, _fullButtonTextSize.x));
                    rect.x -= width;
                    rect.width = width;
                    height= _compactButtonTextSize.y + _fullButtonTextSize.y;
                    if (!renderData.IsReverse) rect.y = rect.height - height;
                    rect.height = height;
                    break;
                
                // 左上(右上)
                case TerminalAnchor.Top:
                    width = Mathf.Ceil(_compactButtonTextSize.x + _fullButtonTextSize.x);
                    height = Mathf.Max(_compactButtonTextSize.y, _fullButtonTextSize.y);
                    if (renderData.IsReverse) rect.x = rect.width - width;
                    rect.width = width;
                    rect.height = height;
                    break;
                
                // 右下(左下)
                case TerminalAnchor.Bottom:
                    width = Mathf.Ceil(_compactButtonTextSize.x + _fullButtonTextSize.x);
                    height = Mathf.Max(_compactButtonTextSize.y, _fullButtonTextSize.y);
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
                    DrawButtons();
                }
            }
        }

        private void Recalculate()
        {
            _compactButtonTextSize = CalcCompactButtonTextSize();
            _fullButtonTextSize = CalcFullButtonTextSize();
        }

        private void DrawButtons()
        {
            if (GUILayout.Button(CompactButtonText, _styleProvider.GetStyle()))
            {
                OnClickCompactOpenButton?.Invoke();
            }
            else if (GUILayout.Button(FullButtonText, _styleProvider.GetStyle()))
            {
                OnClickFullOpenButton?.Invoke();
            }
        }

        private Vector2 CalcCompactButtonTextSize() =>
            _styleProvider.GetStyle().CalcSize(new GUIContent(CompactButtonText));

        private Vector2 CalcFullButtonTextSize() =>
            _styleProvider.GetStyle().CalcSize(new GUIContent(FullButtonText));
        
        private void OnStyleChanged()
        {
            _shouldRecalculation = true;
        }
        
        public void Dispose()
        {
            _styleProvider.OnStyleChanged -= OnStyleChanged;
            
            OnClickCompactOpenButton = null;
            OnClickFullOpenButton = null;
        }
    }
}

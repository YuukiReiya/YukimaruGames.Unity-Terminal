using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalButtonRenderer : ITerminalButtonRenderer
    {
        private readonly IGUIStyleProvider _executeButtonProvider;
        private readonly IGUIStyleProvider _openButtonsProvider;
        private readonly Lazy<float> _widthLazy;
        private readonly Lazy<Vector2> _compactOpenButtonTextSizeLazy;
        private readonly Lazy<Vector2> _fullOpenButtonTextSizeLazy;

        private const string Key = "WindowGUIStyle";
        public string ExecuteButtonText => "| exec";
        public string CompactButtonText=> "[-]";
        public string FullButtonText => "[□]";

        public event Action OnClickExecuteButton;
        public event Action OnClickCompactOpenButton;
        public event Action OnClickFullOpenButton;

        public TerminalButtonRenderer(IPixelTexture2DRepository pixelTexture2DRepository,IGUIStyleProvider executeButtonProvider, IGUIStyleProvider openButtonsProvider)
        {
            _executeButtonProvider = executeButtonProvider;
            _openButtonsProvider = openButtonsProvider;
            
            pixelTexture2DRepository.Add(Key,Color.black);
            _openButtonsProvider.GetStyle().normal.background = pixelTexture2DRepository.GetTexture2D(Key);
            
            _widthLazy = new Lazy<float>(() => Mathf.Ceil(_executeButtonProvider.GetStyle().CalcSize(new GUIContent(ExecuteButtonText)).x));
            
            // NOTE:
            // Lazyで要素に対してMathf.Ceilを組み込んでしまった方が毎フレームの呼び出しを省略できるのだが、BottomレイアウトなどCeilされた結果隙間が生まれてしまうケースがあるため
            // 切り捨てするかどうかは呼び出し側で制御する.
            _compactOpenButtonTextSizeLazy = new Lazy<Vector2>(() => _openButtonsProvider.GetStyle().CalcSize(new GUIContent(CompactButtonText)));
            _fullOpenButtonTextSizeLazy = new Lazy<Vector2>(() => _openButtonsProvider.GetStyle().CalcSize(new GUIContent(FullButtonText)));
        }

        public void ExecuteButtonRender(TerminalButtonRenderData renderData)
        {
            if (!renderData.IsVisible) return;
            
            if (GUILayout.Button(ExecuteButtonText, _executeButtonProvider.GetStyle(),GUILayout.Width(_widthLazy.Value),GUILayout.ExpandWidth(false)))
            {
                OnClickExecuteButton?.Invoke();
            }
        }
        
        public void OpenButtonsRender(TerminalButtonRenderData renderData)
        {
            if (!renderData.IsVisible) return;
            var areaRect = renderData.OpenButtonsRect;
            var anchor = renderData.Anchor;
            
            switch (anchor)
            {
                case TerminalAnchor.Left:
                    areaRect.width = Mathf.Max(Mathf.Ceil(_compactOpenButtonTextSizeLazy.Value.x), Mathf.Ceil(_fullOpenButtonTextSizeLazy.Value.x));
                    areaRect.height = _compactOpenButtonTextSizeLazy.Value.y + _fullOpenButtonTextSizeLazy.Value.y;
                    break;
                case TerminalAnchor.Right:
                    areaRect.width = Mathf.Max(Mathf.Ceil(_compactOpenButtonTextSizeLazy.Value.x), Mathf.Ceil(_fullOpenButtonTextSizeLazy.Value.x));
                    areaRect.height = Mathf.Floor(_compactOpenButtonTextSizeLazy.Value.y + _fullOpenButtonTextSizeLazy.Value.y);
                    
                    areaRect.x -= areaRect.width;
                    areaRect.y -= areaRect.height;
                    break;
                case TerminalAnchor.Top:
                    areaRect.width = Mathf.Ceil(_compactOpenButtonTextSizeLazy.Value.x) + Mathf.Ceil(_fullOpenButtonTextSizeLazy.Value.x);
                    areaRect.height = Mathf.Max(_compactOpenButtonTextSizeLazy.Value.y, _fullOpenButtonTextSizeLazy.Value.y);
                    break;
                case TerminalAnchor.Bottom:
                    areaRect.width = Mathf.Ceil(_compactOpenButtonTextSizeLazy.Value.x) + Mathf.Ceil(_fullOpenButtonTextSizeLazy.Value.x);
                    areaRect.height = Mathf.Max(_compactOpenButtonTextSizeLazy.Value.y, _fullOpenButtonTextSizeLazy.Value.y);
                    
                    areaRect.x -= areaRect.width;
                    areaRect.y -= areaRect.height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            using (new GUILayout.AreaScope(areaRect))
            {
                using (GUI.Scope _ = anchor is TerminalAnchor.Left or TerminalAnchor.Right ?
                           new GUILayout.VerticalScope() :
                           new GUILayout.HorizontalScope())
                {
                    DrawButtons();
                }
            }
        }

        private void DrawButtons()
        {
            if (GUILayout.Button(CompactButtonText, _openButtonsProvider.GetStyle()))
            {
                OnClickCompactOpenButton?.Invoke();
            }
            else if (GUILayout.Button(FullButtonText, _openButtonsProvider.GetStyle()))
            {
                OnClickFullOpenButton?.Invoke();
            }
        }

        private Vector2 Ceil(Vector2 v2) => new(Mathf.Ceil(v2.x), Mathf.Ceil(v2.y));
    }
}

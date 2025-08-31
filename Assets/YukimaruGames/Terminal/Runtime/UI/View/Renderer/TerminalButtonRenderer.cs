using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalButtonRenderer
    {
        string ButtonText { get; }
        void Render(TerminalButtonRenderData renderData);
        event Action OnClickExecuteButton;
    }
    public sealed class TerminalButtonRenderer : ITerminalButtonRenderer
    {
        private readonly IGUIStyleProvider _provider;
        private readonly Lazy<float> _widthLazy;
        
        public string ButtonText => "| exec";
        public bool IsVisible { get; set; }
        public event Action OnClickExecuteButton;
        public TerminalButtonRenderer(IGUIStyleProvider provider)
        {
            _provider = provider;
            _widthLazy = new Lazy<float>(() => Mathf.Ceil(_provider.GetStyle().CalcSize(new GUIContent(ButtonText)).x));
        }

        public void Render(TerminalButtonRenderData renderData)
        {
            if (!renderData.IsVisible) return;
            
            if (GUILayout.Button(ButtonText, _provider.GetStyle(),GUILayout.Width(_widthLazy.Value),GUILayout.ExpandWidth(false)))
            {
                OnClickExecuteButton?.Invoke();
            }
        }
    }
}

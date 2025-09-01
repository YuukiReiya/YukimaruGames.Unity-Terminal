using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalView : ITerminalView,IDisposable
    {
        // renderer.
        private readonly ITerminalWindowRenderer _windowRenderer;
        private readonly ITerminalLogRenderer _logRenderer;
        private readonly ITerminalInputRenderer _inputRenderer;
        private readonly ITerminalPromptRenderer _promptRenderer;
        private readonly ITerminalButtonRenderer _buttonRenderer;
        
        // provider.
        private readonly ITerminalWindowRenderDataProvider _windowRenderDataProvider;
        private readonly ITerminalLogRenderDataProvider _logRenderDataProvider;
        private readonly ITerminalInputRenderDataProvider _inputRenderDataProvider;
        private readonly ITerminalButtonRenderDataProvider _buttonRenderDataProvider;
        private readonly IScrollConfigurator _scrollConfigurator;
        
        // callbacks.
        private readonly List<ITerminalPreRenderer> _preRenderers;
        private readonly List<ITerminalPostRenderer> _postRenderers;
        
        private Vector2Int _size;
        
        public event Action<Vector2Int> OnScreenSizeChanged;
        public event Action OnPreRender;
        public event Action OnPostRender;

        public TerminalView(
            ITerminalWindowRenderer windowRenderer,
            ITerminalLogRenderer logRenderer,
            ITerminalInputRenderer inputRenderer,
            ITerminalPromptRenderer promptRenderer,
            ITerminalButtonRenderer buttonRenderer,
            ITerminalWindowRenderDataProvider windowRenderDataProvider,
            ITerminalLogRenderDataProvider logRenderDataProvider,
            ITerminalInputRenderDataProvider inputRenderDataProvider,
            ITerminalButtonRenderDataProvider buttonRenderDataProvider,
            IScrollConfigurator scrollConfigurator
        )
        {
            _windowRenderer = windowRenderer;
            _logRenderer = logRenderer;
            _inputRenderer = inputRenderer;
            _promptRenderer = promptRenderer;
            _buttonRenderer = buttonRenderer;
            
            _windowRenderDataProvider = windowRenderDataProvider;
            _logRenderDataProvider = logRenderDataProvider;
            _inputRenderDataProvider = inputRenderDataProvider;
            _buttonRenderDataProvider = buttonRenderDataProvider;
            _scrollConfigurator = scrollConfigurator;

            _preRenderers = new object[]
            {
                _windowRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _buttonRenderer,
            }.OfType<ITerminalPreRenderer>().ToList();
            OnPreRender += ExecutePreRender;

            _postRenderers = new object[]
            {
                _windowRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _buttonRenderer,
            }.OfType<ITerminalPostRenderer>().ToList();
            OnPostRender += ExecutePostRender;
        }
        
        /// <inheritdoc/> 
        void ITerminalView.Render()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (_size != size)
            {
                OnScreenSizeChanged?.Invoke(size);
            }
            _size = size;
            
            if (_windowRenderDataProvider == null) return;
            
            _windowRenderer.Render(_windowRenderDataProvider.GetRenderData(), Render);
            _buttonRenderer.OpenButtonsRender(_buttonRenderDataProvider.GetRenderData());
        }

        private void ExecutePreRender()
        {
            for (var i = 0; i < _preRenderers.Count; ++i) _preRenderers[i]?.PreRender();
        }

        private void Render(int id)
        {
            OnPreRender?.Invoke();

            using (new GUILayout.VerticalScope())
            {
                using (var scope =
                       new GUILayout.ScrollViewScope(_scrollConfigurator.ScrollPosition, false, false, GUIStyle.none, GUIStyle.none))
                {
                    _scrollConfigurator.ScrollPosition = scope.scrollPosition;
                    _logRenderer.Render(_logRenderDataProvider.GetRenderData());
                }

                using (new GUILayout.HorizontalScope())
                {
                    _promptRenderer?.Render();
                    _inputRenderer?.Render(_inputRenderDataProvider.GetRenderData());
                    _buttonRenderer?.ExecuteButtonRender(_buttonRenderDataProvider.GetRenderData());
                }
            }

            OnPostRender?.Invoke();
        }

        private void ExecutePostRender()
        {
            for (var i = 0; i < _postRenderers.Count; ++i) _postRenderers[i]?.PostRender();
        }

        public void Dispose()
        {
            OnPreRender -= ExecutePreRender;
            OnPostRender -= ExecutePostRender;
        }
    }
}

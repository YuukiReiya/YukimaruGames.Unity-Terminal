using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalView : ITerminalView, IDisposable
    {
        // renderer.
        private readonly ITerminalWindowRenderer _windowRenderer;
        private readonly ITerminalLogRenderer _logRenderer;
        private readonly ITerminalInputRenderer _inputRenderer;
        private readonly ITerminalPromptRenderer _promptRenderer;
        private readonly ITerminalExecuteButtonRenderer _executeButtonRenderer;
        private readonly ITerminalButtonRenderer _buttonRenderer;
        private readonly ITerminalLogCopyButtonRenderer _logCopyButtonRenderer;
        
        // provider.
        private readonly ITerminalWindowRenderDataProvider _windowRenderDataProvider;
        private readonly ITerminalLogRenderDataProvider _logRenderDataProvider;
        private readonly ITerminalInputRenderDataProvider _inputRenderDataProvider;
        private readonly ITerminalExecuteButtonRenderDataProvider _executeButtonRenderDataProvider;
        private readonly ITerminalButtonRenderDataProvider _buttonRenderDataProvider;
        private readonly IScrollConfigurator _scrollConfigurator;

        // callbacks.
        private readonly List<ITerminalPreRenderer> _preRenderers;
        private readonly List<ITerminalPostRenderer> _postRenderers;

        private Vector2Int _size;

        public event Action<Vector2Int> OnScreenSizeChanged;
        public event Action<string> OnLogCopiedTriggered;
        public event Action OnPreRender;
        public event Action OnPostRender;

        public TerminalView(TerminalViewContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _windowRenderer = context.WindowRenderer;
            _logRenderer = context.LogRenderer;
            _inputRenderer = context.InputRenderer;
            _promptRenderer = context.PromptRenderer;
            _executeButtonRenderer = context.ExecuteButtonRenderer;
            _buttonRenderer = context.ButtonRenderer;
            _logCopyButtonRenderer = context.LogCopyButtonRenderer;
            
            _windowRenderDataProvider = context.WindowRenderDataProvider;
            _logRenderDataProvider = context.LogRenderDataProvider;
            _inputRenderDataProvider = context.InputRenderDataProvider;
            _executeButtonRenderDataProvider = context.ExecuteButtonRenderDataProvider;
            _buttonRenderDataProvider = context.ButtonRenderDataProvider;
            _scrollConfigurator = context.ScrollConfigurator;

            if (_logCopyButtonRenderer != null)
            {
                _logCopyButtonRenderer.OnClickButton += HandleLogCopied;
            }

            var renderers = new object[]
            {
                _windowRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _executeButtonRenderer,
                _buttonRenderer,
            };

            _preRenderers = new List<ITerminalPreRenderer>(renderers.OfType<ITerminalPreRenderer>());
            OnPreRender += ExecutePreRender;

            _postRenderers = new List<ITerminalPostRenderer>(renderers.OfType<ITerminalPostRenderer>());
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

            // WindowのRect外に描画する.
            _buttonRenderer.Render(_buttonRenderDataProvider.GetRenderData());
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
                    _executeButtonRenderer?.Render(_executeButtonRenderDataProvider.GetRenderData());
                }
            }

            OnPostRender?.Invoke();
        }

        private void ExecutePostRender()
        {
            for (var i = 0; i < _postRenderers.Count; ++i) _postRenderers[i]?.PostRender();
        }

        private void HandleLogCopied(string copiedText)
        {
            OnLogCopiedTriggered?.Invoke(copiedText);
        }

        public void Dispose()
        {
            _logCopyButtonRenderer.OnClickButton -= HandleLogCopied;
            
            OnPreRender -= ExecutePreRender;
            OnPostRender -= ExecutePostRender;
            
            _preRenderers.Clear();
            _postRenderers.Clear();
            
            OnScreenSizeChanged = null;
            OnLogCopiedTriggered = null;
        }
    }
}

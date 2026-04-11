using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models;
using YukimaruGames.Terminal.Presentation.Renderers.Launcher;

namespace YukimaruGames.Terminal.Presentation.Renderers.Window
{
    public sealed class TerminalView : ITerminalWindow, IDisposable
    {
        // renderer.
        private readonly IWindowRenderer _windowRenderer;
        private readonly ILogRenderer _logRenderer;
        private readonly IInputRenderer _inputRenderer;
        private readonly IPromptRenderer _promptRenderer;
        private readonly ISubmitRenderer _submitRenderer;
        private readonly ILauncherRenderer _launcherRenderer;
        private readonly IClipboardRenderer _clipboardRenderer;
        
        // provider.
        private readonly IWindowRenderDataProvider _windowRenderDataProvider;
        private readonly ILogRenderDataProvider _logRenderDataProvider;
        private readonly IInputRenderDataProvider _inputRenderDataProvider;
        private readonly ISubmitRenderDataProvider _submitRenderDataProvider;
        private readonly ILauncherRenderDataProvider _buttonRenderDataProvider;
        private readonly IScrollAccessor _scrollAccessor;

        // callbacks.
        private readonly List<IPreRenderer> _preRenderers;
        private readonly List<IPostRenderer> _postRenderers;

        private Vector2Int _size;

        public event Action<Vector2Int> OnScreenSizeChanged;
        public event Action<string> OnLogCopiedTriggered;
        public event Action OnPreRender;
        public event Action OnPostRender;

        public TerminalView(ViewContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _windowRenderer = context.WindowRenderer;
            _logRenderer = context.LogRenderer;
            _inputRenderer = context.InputRenderer;
            _promptRenderer = context.PromptRenderer;
            _submitRenderer = context.SubmitRenderer;
            _launcherRenderer = context.LauncherRenderer;
            _clipboardRenderer = context.ClipboardRenderer;
            
            _windowRenderDataProvider = context.WindowRenderDataProvider;
            _logRenderDataProvider = context.LogRenderDataProvider;
            _inputRenderDataProvider = context.InputRenderDataProvider;
            _submitRenderDataProvider = context.SubmitRenderDataProvider;
            _buttonRenderDataProvider = context.LauncherRenderDataProvider;
            _scrollAccessor = context.ScrollAccessor;

            if (_clipboardRenderer != null)
            {
                _clipboardRenderer.OnClickButton += HandleLogCopied;
            }

            var renderers = new object[]
            {
                _windowRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _submitRenderer,
                _launcherRenderer,
            };

            _preRenderers = new List<IPreRenderer>(renderers.OfType<IPreRenderer>());
            OnPreRender += ExecutePreRender;

            _postRenderers = new List<IPostRenderer>(renderers.OfType<IPostRenderer>());
            OnPostRender += ExecutePostRender;
        }

        /// <inheritdoc/> 
        void ITerminalWindow.Render()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (_size != size)
            {
                OnScreenSizeChanged?.Invoke(size);
            }
            _size = size;

            if (_windowRenderDataProvider == null) return;

            _windowRenderer.Render(_windowRenderDataProvider.RenderData, Render);

            // WindowのRect外に描画する.
            _launcherRenderer.Render(_buttonRenderDataProvider.RenderData);
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
                       new GUILayout.ScrollViewScope(_scrollAccessor.ScrollPosition, false, false, GUIStyle.none, GUIStyle.none))
                {
                    _scrollAccessor.ScrollPosition = scope.scrollPosition;
                    _logRenderer.Render(_logRenderDataProvider.RenderData);
                }

                using (new GUILayout.HorizontalScope())
                {
                    _promptRenderer?.Render();
                    _inputRenderer?.Render(_inputRenderDataProvider.RenderData);
                    _submitRenderer?.Render(_submitRenderDataProvider.RenderData);
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

        void IDisposable.Dispose()
        {
            if (_clipboardRenderer != null)
            {
                _clipboardRenderer.OnClickButton -= HandleLogCopied;
            }

            OnPreRender -= ExecutePreRender;
            OnPostRender -= ExecutePostRender;
            
            _preRenderers?.Clear();
            _postRenderers?.Clear();
            
            OnScreenSizeChanged = null;
            OnLogCopiedTriggered = null;
        }
    }
}

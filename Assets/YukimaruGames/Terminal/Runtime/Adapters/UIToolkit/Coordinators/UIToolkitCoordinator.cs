#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.UIToolkit.Coordinators
{
    /// <summary>
    /// UIToolkit版の<see cref="ITerminalGUI"/>実装.
    /// </summary>
    /// <remarks>
    /// <see cref="Adapters.IMGUI.Coordinators.TerminalIMGUI"/>と同様、各Presenter(Providerで公開される
    /// 描画データ)を毎フレーム各Rendererへ差分プッシュする役割を担う。UIToolkitはリテインドモードのため、
    /// GUILayoutのスコープ制御や<see cref="IWindowRenderer"/>相当のウィンドウ開閉処理は不要で、
    /// <see cref="WindowRoot"/>へRectを反映するだけで足りる.
    /// </remarks>
    public sealed class UIToolkitCoordinator : ITerminalGUI, IDisposable
    {
        private readonly WindowRoot _windowRoot;
        private readonly ILogRenderer _logRenderer;
        private readonly IInputRenderer _inputRenderer;
        private readonly IPromptRenderer _promptRenderer;
        private readonly ISubmitRenderer _submitRenderer;
        private readonly ILauncherRenderer _launcherRenderer;
        private readonly IClipboardRenderer _clipboardRenderer;

        private readonly IWindowRenderDataProvider _windowRenderDataProvider;
        private readonly ILogRenderDataProvider _logRenderDataProvider;
        private readonly IInputRenderDataProvider _inputRenderDataProvider;
        private readonly ISubmitRenderDataProvider _submitRenderDataProvider;
        private readonly ILauncherRenderDataProvider _launcherRenderDataProvider;
        private readonly IScrollProvider _scrollProvider;
        private readonly ScrollView _scrollView;

        private Vector2Int _size;

        public event Action<Vector2Int> OnScreenSizeChanged;
        public event Action<string> OnLogCopiedTriggered;
        public event Action OnPreRender;
        public event Action OnPostRender;

        public UIToolkitCoordinator(
            WindowRoot windowRoot,
            ILogRenderer logRenderer,
            IInputRenderer inputRenderer,
            IPromptRenderer promptRenderer,
            ISubmitRenderer submitRenderer,
            ILauncherRenderer launcherRenderer,
            IClipboardRenderer clipboardRenderer,
            IWindowRenderDataProvider windowRenderDataProvider,
            ILogRenderDataProvider logRenderDataProvider,
            IInputRenderDataProvider inputRenderDataProvider,
            ISubmitRenderDataProvider submitRenderDataProvider,
            ILauncherRenderDataProvider launcherRenderDataProvider,
            IScrollProvider scrollProvider)
        {
            _windowRoot = windowRoot;
            _logRenderer = logRenderer;
            _inputRenderer = inputRenderer;
            _promptRenderer = promptRenderer;
            _submitRenderer = submitRenderer;
            _launcherRenderer = launcherRenderer;
            _clipboardRenderer = clipboardRenderer;

            _windowRenderDataProvider = windowRenderDataProvider;
            _logRenderDataProvider = logRenderDataProvider;
            _inputRenderDataProvider = inputRenderDataProvider;
            _submitRenderDataProvider = submitRenderDataProvider;
            _launcherRenderDataProvider = launcherRenderDataProvider;
            _scrollProvider = scrollProvider;
            _scrollView = _windowRoot != null ? _windowRoot.LogScrollView : null;

            if (_clipboardRenderer != null)
            {
                _clipboardRenderer.OnClickButton += HandleLogCopied;
            }

            if (_scrollProvider != null)
            {
                _scrollProvider.OnScrollChanged += HandleScrollChanged;
            }
        }

        void ITerminalGUI.Render()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (_size != size)
            {
                OnScreenSizeChanged?.Invoke(size);
            }

            _size = size;

            if (_windowRoot == null || !_windowRoot.IsInitialized) return;

            OnPreRender?.Invoke();

            _windowRoot.ApplyRect(_windowRenderDataProvider.RenderData.Rect);
            _logRenderer?.Render(_logRenderDataProvider.RenderData);
            _promptRenderer?.Render();
            _inputRenderer?.Render(_inputRenderDataProvider.RenderData);
            _submitRenderer?.Render(_submitRenderDataProvider.RenderData);
            _launcherRenderer?.Render(_launcherRenderDataProvider.RenderData);

            OnPostRender?.Invoke();
        }

        private void HandleScrollChanged(Vector2 position)
        {
            if (_scrollView == null) return;

            _scrollView.scrollOffset = Mathf.Approximately(position.y, float.MaxValue)
                ? new Vector2(_scrollView.scrollOffset.x, _scrollView.contentContainer.layout.height)
                : position;
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

            if (_scrollProvider != null)
            {
                _scrollProvider.OnScrollChanged -= HandleScrollChanged;
            }

            OnScreenSizeChanged = null;
            OnLogCopiedTriggered = null;
            OnPreRender = null;
            OnPostRender = null;
        }
    }
}
#endif

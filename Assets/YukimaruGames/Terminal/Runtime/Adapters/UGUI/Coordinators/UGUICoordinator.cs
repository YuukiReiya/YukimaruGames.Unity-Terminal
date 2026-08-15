#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UI;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Adapters.UGUI.Coordinators
{
    /// <summary>
    /// uGUI版の<see cref="ITerminalGUI"/>実装.
    /// </summary>
    /// <remarks>
    /// 各Presenter(Providerで公開される描画データ)を毎フレーム各Rendererへ差分プッシュする。
    /// uGUIはリテインドモードのため、GUILayoutのスコープ制御や<see cref="IWindowRenderer"/>相当の
    /// ウィンドウ描画は不要で、<see cref="WindowRoot"/>へRectを反映するだけで足りる
    /// (UIToolkit版と同じ構成).
    /// </remarks>
    public sealed class UGUICoordinator : ITerminalGUI, IDisposable
    {
        /// <summary>
        /// 末尾追従を再試行するフレーム数.
        /// </summary>
        /// <remarks>
        /// 新しいログ行が実際にContentへ反映されるまでのフレーム数は、非同期コマンドの内容次第で
        /// 可変。1フレームでは足りず、固定回数の再試行が要る(#122).
        /// </remarks>
        private const int ScrollToEndRetryFrames = 20;

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
        private readonly IScrollAccessor _scrollAccessor;
        private readonly ScrollRect _scrollRect;

        private Vector2Int _size;
        private int _scrollToEndRetriesRemaining;

        public event Action<Vector2Int> OnScreenSizeChanged;
        public event Action<string> OnLogCopiedTriggered;
        public event Action OnPreRender;
        public event Action OnPostRender;

        public UGUICoordinator(
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
            IScrollAccessor scrollAccessor)
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
            _scrollAccessor = scrollAccessor;
            _scrollRect = _windowRoot != null ? _windowRoot.LogScrollView : null;

            if (_clipboardRenderer != null)
            {
                _clipboardRenderer.OnClickButton += HandleLogCopied;
            }

            if (_scrollAccessor != null)
            {
                _scrollAccessor.OnScrollChanged += HandleScrollChanged;
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

            ProcessScrollToEndRetry();
            SyncScrollPosition();

            OnPostRender?.Invoke();
        }

        /// <summary>
        /// 実際のスクロール位置を<see cref="IScrollAccessor"/>へ書き戻す.
        /// </summary>
        /// <remarks>
        /// <c>ScrollToEnd()</c>はセンチネル(<c>float.MaxValue</c>)で「既に末尾スクロール要求済み」を
        /// 判定しており、書き戻しが無いと最初の1回以降の自動追従が恒久的にno-opになる(#122で
        /// UIToolkit版が踏んだ)。通知を伴わない<c>SyncPosition()</c>を使うこと
        /// (<c>ScrollPosition</c>のセッターだと相互フィードバックで入力ラグになる).
        /// </remarks>
        private void SyncScrollPosition()
        {
            if (_scrollAccessor == null || _scrollRect == null) return;

            _scrollAccessor.SyncPosition(new Vector2(0f, GetVerticalOffsetFromTop()));
        }

        /// <summary>
        /// 末尾追従の再試行を1フレーム分進める.
        /// </summary>
        /// <remarks>
        /// コルーチンやスケジューラではなくフレームカウンタで実装している。本クラスは
        /// MonoBehaviourではなくコルーチンを直接持てないうえ、カウンタなら「再試行は常に1本だけ」
        /// という性質が構造上保証され、再登録時・Dispose時の停止漏れも起きないため
        /// (UIToolkit版はスケジュール項目を1つだけ保持する形で同じ性質を担保している).
        /// </remarks>
        private void ProcessScrollToEndRetry()
        {
            if (_scrollToEndRetriesRemaining <= 0 || _scrollRect == null) return;

            --_scrollToEndRetriesRemaining;

            // 追加された行のレイアウトを確定させてからでないと、スクロール範囲が0のままとなり
            // 末尾へ移動できない.
            Canvas.ForceUpdateCanvases();

            // uGUIのverticalNormalizedPositionは0が末尾(下端).
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        /// 上端からのスクロール量(px)を求める.
        /// </summary>
        /// <remarks>
        /// IMGUI版・UIToolkit版が扱うのは左上原点のピクセル座標のため、uGUIの正規化位置
        /// (0が下端)から変換して揃える.
        /// </remarks>
        private float GetVerticalOffsetFromTop()
        {
            var content = _scrollRect.content;
            var viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollRect.GetComponent<RectTransform>();
            if (content == null || viewport == null) return 0f;

            var scrollable = content.rect.height - viewport.rect.height;
            if (scrollable <= 0f) return 0f;

            return (1f - Mathf.Clamp01(_scrollRect.verticalNormalizedPosition)) * scrollable;
        }

        private void HandleScrollChanged(Vector2 position)
        {
            if (_scrollRect == null) return;

            // 再登録するだけで前回分は上書きされる(カウンタのため多重実行が起きない).
            _scrollToEndRetriesRemaining = ScrollToEndRetryFrames;
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

            if (_scrollAccessor != null)
            {
                _scrollAccessor.OnScrollChanged -= HandleScrollChanged;
            }

            _scrollToEndRetriesRemaining = 0;

            OnScreenSizeChanged = null;
            OnLogCopiedTriggered = null;
            OnPreRender = null;
            OnPostRender = null;
        }
    }
}
#endif

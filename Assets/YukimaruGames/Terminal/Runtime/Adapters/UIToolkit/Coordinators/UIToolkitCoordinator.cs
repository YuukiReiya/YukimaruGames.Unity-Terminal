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
        private readonly IScrollAccessor _scrollAccessor;
        private readonly ScrollView _scrollView;

        private Vector2Int _size;
        private IVisualElementScheduledItem _scrollToEndScheduledItem;

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
            _scrollView = _windowRoot != null ? _windowRoot.LogScrollView : null;

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

            // ScrollAccessor.ScrollToEnd()はセンチネル(float.MaxValue)で「既に末尾スクロール要求済み」
            // を判定しており、一度到達すると以降呼び出しても早期returnする。IMGUI版
            // (TerminalIMGUI.cs)はGUILayout.ScrollViewScope経由で実際のスクロール位置を毎フレーム
            // ScrollAccessorへ書き戻すことでこのセンチネルを都度リセットしているが、UIToolkit版には
            // その書き戻しが存在しなかったため、最初の1回のScrollToEnd()以降、コマンド実行のたびの
            // 自動追従が恒久的にno-opになっていた(#122)。ScrollViewの実際の(クランプ済み)
            // scrollOffsetを毎フレーム書き戻し、IMGUI版と同じくセンチネルを都度リセットさせる.
            if (_scrollAccessor != null && _scrollView != null)
            {
                _scrollAccessor.SyncPosition(_scrollView.scrollOffset);
            }

            OnPostRender?.Invoke();
        }

        private const int ScrollToEndRetryFrames = 20;

        private void HandleScrollChanged(Vector2 position)
        {
            if (_scrollView == null) return;

            // コマンド実行(新しいログ行の追加)と同じフレーム内でScrollToEnd()が呼ばれるが、
            // 実際のログ行は非同期のコマンド実行(ExecuteAsync)が完了して初めてScrollViewへ
            // 追加されるため、追加が完了するまでのフレーム数は可変(コマンドの内容次第)である。
            // 固定1ティックの遅延(ExecuteLater(0))では、遅延実行時点でまだ新しい行が
            // ScrollViewへ反映されておらずverticalScroller.highValueが0のまま(実機ログで確認)、
            // という不具合が起きていた(#122)。実際に新しい行が反映されhighValueが確定するまで
            // 複数フレームに渡って再試行する.
            //
            // 短時間に連続でコマンドが実行されると、前回登録した再試行項目がまだ完了していない
            // うちに次のHandleScrollChangedが呼ばれうる。項目を都度使い捨てのローカル変数のまま
            // 積み増すと、複数の項目が並行してscrollOffsetへ書き込み続け、Dispose後も実行中の
            // 項目が残ってしまう(コードレビューで指摘)。コーディネーターで1つだけ保持し、
            // 新規登録時・Dispose時に必ずPause()する.
            _scrollToEndScheduledItem?.Pause();

            var scrollView = _scrollView;
            var attempts = 0;
            _scrollToEndScheduledItem = scrollView.schedule.Execute(() =>
            {
                if (scrollView?.panel == null)
                {
                    _scrollToEndScheduledItem?.Pause();
                    return;
                }

                scrollView.scrollOffset = position;
                ++attempts;

                // highValueが0(=まだ新しい行が反映されていない)場合とhighValueに実際に到達した
                // 場合を、scrollOffsetとhighValueの一致だけでは区別できない(どちらもscrollOffsetが
                // highValueにクランプされ一致して見える)ため、判定に頼らず固定回数フレーム
                // 再試行してから止める.
                if (attempts >= ScrollToEndRetryFrames)
                {
                    _scrollToEndScheduledItem?.Pause();
                }
            }).Every(0);
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

            _scrollToEndScheduledItem?.Pause();
            _scrollToEndScheduledItem = null;

            OnScreenSizeChanged = null;
            OnLogCopiedTriggered = null;
            OnPreRender = null;
            OnPostRender = null;
        }
    }
}
#endif

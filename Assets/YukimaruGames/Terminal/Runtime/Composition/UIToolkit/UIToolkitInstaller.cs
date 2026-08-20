#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Adapters.UIToolkit;
using YukimaruGames.Terminal.Adapters.UIToolkit.Coordinators;
using YukimaruGames.Terminal.Adapters.UIToolkit.Renderers;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Infrastructure.Accessors;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Presentation.Accessors;
using YukimaruGames.Terminal.Presentation.Animators;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Presenters;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// UIToolkit(<see cref="UIDocument"/>/<see cref="VisualElement"/>)ベースの実装を構築する
    /// <see cref="IInstaller"/>実装.
    /// </summary>
    /// <remarks>
    /// <see cref="ImmediateModeInstaller"/>との差分は描画コンテキストの構築(<see cref="BuildRenderingContext"/>)
    /// のみであり、Domain層・入力・Scopeの構築やInspector設定の再同期は<see cref="InstallerBase"/> /
    /// <see cref="GraphicalInstallerBase"/>と
    /// 共有する(#122 / #137 / #145)。UIToolkit要素の生成は<see cref="UIToolkitViewFactory"/>、
    /// テーマ・フォントの適用は<see cref="UIToolkitThemeApplier"/>に委ねる。
    /// <c>com.unity.modules.uielements</c>が利用できない環境では本クラスごとコンパイル対象外になる
    /// (<c>TERMINAL_UITOOLKIT_AVAILABLE</c> / #125参照)。
    /// </remarks>
    [Serializable, AddTypeMenu("UIToolkit Installer")]
    public sealed class UIToolkitInstaller : GraphicalInstallerBase
    {
        // 現状はIMGUI版と同じテーマ設定を共有している。UIToolkitの見た目をUSS駆動へ寄せて
        // このフィールドを落とすことは別Issueで検討する(#145のスコープ外).
        [SerializeReference, SerializeInterface]
        private ITerminalTheme _theme = new ImmediateModeTheme();

        [Header("UIToolkit Assets (未指定時はコード生成の最小限UIにフォールバック)")]
        [SerializeField] private VisualTreeAsset _visualTreeAsset;
        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private PanelSettings _panelSettings;

        [Header("UIToolkit Options (このバックエンド固有の設定)")]
        [SerializeField] private UIToolkitOptions _uiToolkitOptions = new();

        /// <summary>
        /// UIToolkitバックエンド固有の設定。<see cref="ITerminalOptions"/>はIMGUI版と共有の
        /// 抽象であり、UIToolkitの<see cref="ScrollView"/>固有の値(マウスホイール感度等)を
        /// 混ぜ込むのは不適切なため、専用のシリアライズ可能な設定ブロックとして分離する(#122).
        /// </summary>
        [Serializable]
        private sealed class UIToolkitOptions
        {
            [Tooltip("マウスホイール1クリックあたりのスクロール量(px)。既定はUIToolkitの標準値(18)。")]
            [SerializeField] private float _scrollSensitivity = 18f;

            [Tooltip("慣性スクロールの減速率。0に近いほど長く滑る。")]
            [SerializeField] private float _scrollDecelerationRate = 0.135f;

            public float ScrollSensitivity => _scrollSensitivity;
            public float ScrollDecelerationRate => _scrollDecelerationRate;
        }

        #region runtime-instances

        [NonSerialized] private IColorPaletteAccessor _colorPaletteAccessor;
        [NonSerialized] private ICursorFlashSpeedAccessor _cursorFlashSpeedAccessor;
        [NonSerialized] private UIToolkitThemeApplier _themeApplier;

        // 生成したWindowRoot(UIDocumentを載せたGameObjectを所有する)。通常はComponents経由で
        // TerminalRuntimeScopeが破棄するが、BuildRenderingContext()がここより後で例外を投げると
        // Componentsが未確定のままスコープ経由の破棄が走らず、GameObjectが取り残される。
        // 保険としてここでも保持しClearReferences()から破棄する
        // (WindowRoot.Disposeは破棄済みチェック付きで、二重呼び出しは無害).
        [NonSerialized] private WindowRoot _windowRoot;

        // 実行時生成したPanelSettingsの解放ハンドル。通常はRenderingContextのComponents経由で
        // TerminalRuntimeScopeが破棄するが、BuildRenderingContext()が最後まで到達せずComponentsが
        // 未確定のまま失敗した場合はScope経由の破棄が走らないため、保険としてここでも保持し
        // ClearReferences()から破棄する(RuntimeGeneratedAsset.Disposeは冪等).
        [NonSerialized] private RuntimeGeneratedAsset _generatedPanelSettings;

        #endregion

        /// <inheritdoc/>
        protected override void ClearReferences()
        {
            _colorPaletteAccessor = null;
            _cursorFlashSpeedAccessor = null;
            _themeApplier = null;

            // MonoBehaviourのため == null で判定する(破棄済みを検出できない ?. は使わない).
            if (_windowRoot != null)
            {
                ((IDisposable)_windowRoot).Dispose();
            }

            _windowRoot = null;

            _generatedPanelSettings?.Dispose();
            _generatedPanelSettings = null;

            base.ClearReferences();
        }

        /// <inheritdoc/>
        protected override void OnResolve()
        {
            base.OnResolve();

            ApplyTheme(_theme ?? new NullTheme());
        }

        /// <summary>
        /// テーマ設定を実行時インスタンスへ再適用する.
        /// </summary>
        private void ApplyTheme(ITerminalTheme theme)
        {
            ThemeBinder.Apply(theme, _colorPaletteAccessor, _cursorFlashSpeedAccessor);

            var uiToolkitOptions = _uiToolkitOptions ?? new UIToolkitOptions();
            _themeApplier?.Apply(theme, ScreenHeight, uiToolkitOptions.ScrollSensitivity, uiToolkitOptions.ScrollDecelerationRate);
        }

        /// <summary>
        /// <see cref="WindowAnimator"/>はOpen/Close遷移中、アンカーに応じて位置(X/Y)を毎フレーム
        /// 徐々に変化させるスライド方式で動く。過去の実機検証で、UIToolkit側のクリップ領域キャッシュが
        /// 不整合を起こし、アニメーション完了後も一部領域が描画されなくなる不具合が確認されたため
        /// (#122)、一時的にDuration=0(瞬時に開閉、スライド演出なし)へ固定して回避していた。
        /// その後、当時の不具合と絡んでいた可能性のある別の原因(LogRendererの毎フレーム無条件
        /// 再描画によるレイアウトの継続的なdirty化、ScrollViewのcontentContainer圧縮等)を修正した
        /// ため、<see cref="ITerminalAnimation.Duration"/>(IMGUI版と共有の設定)を再び尊重するよう
        /// 戻す。もし同様の描画崩れが再発する場合は、このコメントを参照のうえDuration=0固定に
        /// 戻すことを検討すること.
        /// </summary>
        protected override RenderingContext BuildRenderingContext(ITerminalAnimation animation, ITerminalOptions options, in DomainContext domain)
        {
            var theme = _theme ?? new NullTheme();

            var windowAnimationAccessor = CreateWindowAnimationAccessor(animation);
            _colorPaletteAccessor = ThemeBinder.CreateColorPalette(theme);
            var launcherVisibleAccessor = CreateLauncherVisibleAccessor(options);
            var scrollAccessor = new ScrollAccessor();

            var (windowRoot, generatedPanelSettings) =
                UIToolkitViewFactory.Create(_visualTreeAsset, _styleSheet, _panelSettings);
            _windowRoot = windowRoot;
            _generatedPanelSettings = generatedPanelSettings;

            var cursorView = new CursorView();
            var clipboardRenderer = new ClipboardRenderer(launcherVisibleAccessor);
            var logRenderer = new LogRenderer(windowRoot.LogScrollView, clipboardRenderer, _colorPaletteAccessor, launcherVisibleAccessor, theme.CopyButtonColor);

            // LogRendererを渡してから適用する(Apply内でLogRendererのフォント・コピーボタン色も同期するため).
            _themeApplier = new UIToolkitThemeApplier(windowRoot) { LogRenderer = logRenderer };
            ApplyTheme(theme);

            var inputRenderer = new InputRenderer(windowRoot.InputField, scrollAccessor, cursorView);
            var promptRenderer = new PromptRenderer(windowRoot.PromptLabel, domain.Service)
            {
                ShowLoadingIndicator = options.ShowLoadingIndicator,
                LoadingIndicatorFrames = options.LoadingIndicatorFrames,
            };
            PromptRenderer = promptRenderer;
            var submitRenderer = new SubmitRenderer(windowRoot.SubmitButton);
            var launcherRenderer = new LauncherRenderer(windowRoot.LauncherContainer, windowRoot.LauncherOpenButton, windowRoot.LauncherCloseButton);

            var windowPresenter = new WindowPresenter(
                windowAnimationAccessor,
                new WindowAnimator(),
                new ScreenSizeAccessor(),
                new UnityExceptionLogger());

            // WindowPresenter.Rectは既定でTerminalRectのdefault値(全フィールド0)のままであり、
            // 実際の(起動時の開閉状態を反映した)Rectは最初のUIToolkitCoordinator.Render()呼び出し
            // (Update()駆動)まで計算・反映されない。UIDocumentのGameObject自体はAwake()時点で
            // 既に生成・アクティブ化されているため、その間の1フレーム、WindowRootのRootが
            // 位置・サイズ未指定のまま(=既定の見た目)で描画されてしまい、PlayMode起動直後に
            // ウィンドウが一瞬フルサイズ相当で見えてしまう不具合につながっていた。
            // Refresh()でアニメーションを進めずに現在の状態(起動時の開閉状態)のRectを同期的に
            // 計算し、Awake()完了前(最初のフレームが描画される前)にWindowRootへ反映しておく.
            windowPresenter.Refresh();
            windowRoot.ApplyRect(windowPresenter.Rect);

            _cursorFlashSpeedAccessor = new CursorFlashSpeedAccessor(theme.CursorFlashSpeed);
            var cursorPresenter = new CursorPresenter(_cursorFlashSpeedAccessor, cursorView);
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var submitPresenter = new SubmitPresenter(submitRenderer, launcherVisibleAccessor);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, launcherVisibleAccessor, windowAnimationAccessor);

            // ウィンドウ本体(Root)と同じ理由で、ランチャーボタン([-]/[x])と実行ボタンも
            // 生成直後は表示/位置が未確定(既定でFlex表示・(0,0)相当)のまま最初のUpdate()駆動の
            // Renderまで残るため、PlayMode起動直後に一瞬変な位置・状態で見えてしまう。
            // Awake()完了前(最初のフレームが描画される前)に一度、実際の起動時状態で
            // 同期的にRenderしておく.
            launcherRenderer.Render(((ILauncherRenderDataProvider)launcherPresenter).RenderData);
            submitRenderer.Render(((ISubmitRenderDataProvider)submitPresenter).RenderData);

            var view = new UIToolkitCoordinator(
                windowRoot,
                logRenderer,
                inputRenderer,
                promptRenderer,
                submitRenderer,
                launcherRenderer,
                clipboardRenderer,
                windowPresenter,
                logPresenter,
                inputPresenter,
                submitPresenter,
                launcherPresenter,
                scrollAccessor);

            var terminalView = new TerminalView(windowPresenter);

            return new RenderingContext
            {
                GUI = view,
                ScrollMutator = scrollAccessor,
                WindowAnimationAccessor = windowAnimationAccessor,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                LogPresenter = logPresenter,
                SubmitPresenter = submitPresenter,
                LauncherPresenter = launcherPresenter,
                View = terminalView,

                Components = new object[]
                {
                    windowAnimationAccessor,
                    _colorPaletteAccessor,
                    scrollAccessor,
                    launcherVisibleAccessor,
                    _cursorFlashSpeedAccessor,

                    windowRoot,
                    cursorView,
                    clipboardRenderer,
                    logRenderer,
                    inputRenderer,
                    promptRenderer,
                    submitRenderer,
                    launcherRenderer,

                    windowPresenter,
                    cursorPresenter,
                    logPresenter,
                    inputPresenter,
                    submitPresenter,
                    launcherPresenter,

                    view,
                    terminalView,

                    // UIDocumentを載せたGameObject(windowRoot)の破棄より後に解放されるよう末尾に置く.
                    generatedPanelSettings,
                },
            };
        }
    }
}
#endif

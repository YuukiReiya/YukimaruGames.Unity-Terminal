#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Adapters.UGUI;
using YukimaruGames.Terminal.Adapters.UGUI.Coordinators;
using YukimaruGames.Terminal.Adapters.UGUI.Renderers;
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
    /// uGUI(<see cref="Canvas"/>/<see cref="UnityEngine.UI"/>)ベースの実装を構築する
    /// <see cref="IInstaller"/>実装.
    /// </summary>
    /// <remarks>
    /// Domain層・入力・Scopeの構築やInspector設定の再同期は<see cref="InstallerBase"/> /
    /// <see cref="GraphicalInstallerBase"/>と共有し、ここではuGUI固有の描画コンテキストの
    /// 構築だけを担う(#139)。UI要素の生成は<see cref="UGUIViewFactory"/>、テーマ適用は
    /// <see cref="UGUIThemeApplier"/>に委ねる。
    /// <c>com.unity.ugui</c>が利用できない環境では本クラスごとコンパイル対象外になる.
    /// </remarks>
    [Serializable, AddTypeMenu("uGUI Installer")]
    public sealed class UGUIInstaller : GraphicalInstallerBase
    {
        [SerializeReference, SerializeInterface]
        private ITerminalTheme _theme = new ImmediateModeTheme();

        [Header("uGUI Assets (未指定時はコード生成の最小限UIにフォールバック)")]
        [SerializeField] private GameObject _prefab;

        [Header("uGUI Options (このバックエンド固有の設定)")]
        [SerializeField] private UGUIOptions _uguiOptions = new();

        /// <summary>
        /// uGUIバックエンド固有の設定.
        /// </summary>
        /// <remarks>
        /// <see cref="ITerminalOptions"/>は他バックエンドと共有の抽象であり、
        /// <see cref="Canvas"/>固有の値を混ぜ込むのは不適切なため、専用のシリアライズ可能な
        /// 設定ブロックとして分離する(UIToolkit版と同じ方針).
        /// </remarks>
        [Serializable]
        private sealed class UGUIOptions
        {
            [Tooltip("Canvasの描画順。他のUIより手前に出したい場合に上げる。")]
            [SerializeField] private int _sortingOrder = short.MaxValue;

            public int SortingOrder => _sortingOrder;
        }

        #region runtime-instances

        [NonSerialized] private IColorPaletteAccessor _colorPaletteAccessor;
        [NonSerialized] private ICursorFlashSpeedAccessor _cursorFlashSpeedAccessor;
        [NonSerialized] private UGUIThemeApplier _themeApplier;

        // 生成したWindowRoot(Canvasを載せたGameObjectを所有する)。通常はComponents経由で
        // TerminalRuntimeScopeが破棄するが、BuildRenderingContext()がここより後で例外を投げると
        // Componentsが未確定のままスコープ経由の破棄が走らず、GameObjectが取り残される。
        // 保険としてここでも保持しClearReferences()から破棄する.
        [NonSerialized] private WindowRoot _windowRoot;

        // 自前生成したEventSystemの解放ハンドル(既存があった場合はnull)。上と同じ理由で保持する.
        [NonSerialized] private RuntimeGeneratedAsset _generatedEventSystem;

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

            _generatedEventSystem?.Dispose();
            _generatedEventSystem = null;

            base.ClearReferences();
        }

        /// <inheritdoc/>
        protected override void OnResolve()
        {
            base.OnResolve();

            ApplyTheme(_theme ?? new NullTheme());
        }

        /// <inheritdoc/>
        protected override RenderingContext BuildRenderingContext(ITerminalAnimation animation, ITerminalOptions options, in DomainContext domain)
        {
            var theme = _theme ?? new NullTheme();
            var uguiOptions = _uguiOptions ?? new UGUIOptions();

            var windowAnimationAccessor = CreateWindowAnimationAccessor(animation);
            _colorPaletteAccessor = ThemeBinder.CreateColorPalette(theme);
            var launcherVisibleAccessor = CreateLauncherVisibleAccessor(options);
            var scrollAccessor = new ScrollAccessor();

            var useInputSystemModule = ResolveKeyboardType(options) == InputKeyboardType.InputSystem;
            var (windowRoot, generatedEventSystem) =
                UGUIViewFactory.Create(_prefab, uguiOptions.SortingOrder, useInputSystemModule);
            _windowRoot = windowRoot;
            _generatedEventSystem = generatedEventSystem;

            var cursorView = new CursorView();
            var clipboardRenderer = new ClipboardRenderer(launcherVisibleAccessor);
            var logRenderer = new LogRenderer(
                windowRoot.LogContent,
                clipboardRenderer,
                _colorPaletteAccessor,
                launcherVisibleAccessor,
                theme.CopyButtonColor);

            // LogRendererを渡してから適用する(Apply内でLogRendererのフォントも同期するため).
            _themeApplier = new UGUIThemeApplier(windowRoot) { LogRenderer = logRenderer };
            ApplyTheme(theme);

            var inputRenderer = new InputRenderer(windowRoot.InputField, scrollAccessor, cursorView);
            var promptRenderer = new PromptRenderer(windowRoot.PromptLabel, domain.Service)
            {
                ShowLoadingIndicator = options.ShowLoadingIndicator,
                LoadingIndicatorFrames = options.LoadingIndicatorFrames,
            };
            PromptRenderer = promptRenderer;
            var submitRenderer = new SubmitRenderer(windowRoot.SubmitButton);
            var launcherRenderer = new LauncherRenderer(
                windowRoot.LauncherContainer,
                windowRoot.LauncherOpenButton,
                windowRoot.LauncherCloseButton);

            var windowPresenter = new WindowPresenter(
                windowAnimationAccessor,
                new WindowAnimator(),
                new ScreenSizeAccessor(),
                new UnityExceptionLogger());

            // 起動直後の1フレーム、Rectが未計算(全フィールド0)のまま描画されるのを避けるため、
            // アニメーションを進めずに現在の状態のRectを同期的に計算して反映しておく(#122).
            windowPresenter.Refresh();
            windowRoot.ApplyRect(windowPresenter.Rect);

            _cursorFlashSpeedAccessor = new CursorFlashSpeedAccessor(theme.CursorFlashSpeed);
            var cursorPresenter = new CursorPresenter(_cursorFlashSpeedAccessor, cursorView);
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var submitPresenter = new SubmitPresenter(submitRenderer, launcherVisibleAccessor);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, launcherVisibleAccessor, windowAnimationAccessor);

            // ウィンドウ本体と同じ理由で、ランチャーボタンと実行ボタンも起動直後の状態で
            // 一度同期的にRenderしておく.
            launcherRenderer.Render(((ILauncherRenderDataProvider)launcherPresenter).RenderData);
            submitRenderer.Render(((ISubmitRenderDataProvider)submitPresenter).RenderData);

            var view = new UGUICoordinator(
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

                    // Canvasを載せたGameObject(windowRoot)の破棄より後に解放されるよう末尾に置く.
                    _generatedEventSystem,
                },
            };
        }

        /// <summary>
        /// テーマ設定を実行時インスタンスへ再適用する.
        /// </summary>
        private void ApplyTheme(ITerminalTheme theme)
        {
            ThemeBinder.Apply(theme, _colorPaletteAccessor, _cursorFlashSpeedAccessor);
            _themeApplier?.Apply(theme);
        }
    }
}
#endif

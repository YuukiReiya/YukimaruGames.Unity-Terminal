#if TERMINAL_UITOOLKIT_AVAILABLE
#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Composition.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Adapters.UIToolkit;
using YukimaruGames.Terminal.Adapters.UIToolkit.Coordinators;
using YukimaruGames.Terminal.Adapters.UIToolkit.Renderers;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Repositories;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.Infrastructure.Accessors;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Infrastructure.Discoverer;
using YukimaruGames.Terminal.Infrastructure.Factories;
using YukimaruGames.Terminal.Infrastructure.Modes;
using YukimaruGames.Terminal.Presentation.Accessors;
using YukimaruGames.Terminal.Presentation.Animators;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Coordinators;
using YukimaruGames.Terminal.Presentation.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// UIToolkit(<see cref="UIDocument"/>/<see cref="VisualElement"/>)ベースの実装を構築する
    /// <see cref="IInstaller"/>実装.
    /// </summary>
    /// <remarks>
    /// <see cref="ImmediateModeInstaller"/>との差分は描画コンテキストの構築(<see cref="BuildRenderingContext"/>相当)
    /// のみであり、DomainContext/CoordinatorContextの構築は同じ構成を踏襲する(#122参照)。
    /// <c>com.unity.modules.uielements</c>が利用できない環境では本クラスごとコンパイル対象外になる
    /// (<c>TERMINAL_UITOOLKIT_AVAILABLE</c> / #125参照)。
    /// </remarks>
    [Serializable, AddTypeMenu("UIToolkit Installer")]
    public sealed class UIToolkitInstaller : IInstaller
    {
        #region inner-struct

        private struct DomainContext
        {
            public IReadOnlyList<object> Components;
            public ITerminalService Service;
            public ICommandLogger Logger;
            public ICommandHistory History;
            public ICommandRegistry Registry;
            public ICommandAutocomplete Autocomplete;
            public ICommandDiscoverer Discoverer;
            public IExecuteCommandUseCase UseCase;
        }

        private struct RenderingContext
        {
            public IReadOnlyList<object> Components;
            public ITerminalGUI GUI;
            public IScrollMutator ScrollMutator;
            public IWindowAnimationAccessor WindowAnimationAccessor;
            public IWindowPresenter WindowPresenter;
            public IInputPresenter InputPresenter;
            public ILogPresenter LogPresenter;
            public ISubmitPresenter SubmitPresenter;
            public ILauncherPresenter LauncherPresenter;
            public ITerminalView View { get; set; }
        }

        private struct CoordinatorContext
        {
            public IReadOnlyList<object> Components;
            public TerminalCoordinator Coordinator;
            public IEventListener EventListener;
        }

        #endregion

        [SerializeReference, SerializeInterface]
        private ITerminalTheme _theme = new ImmediateModeTheme();

        [SerializeReference, SerializeInterface]
        private ITerminalAnimation _animation = new ImmediateModeAnimation();

        [SerializeReference, SerializeInterface]
        private ITerminalOptions _options = new ImmediateModeOptions();

        [Header("UIToolkit Assets (未指定時はコード生成の最小限UIにフォールバック)")]
        [SerializeField] private VisualTreeAsset _visualTreeAsset;
        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private PanelSettings _panelSettings;

        [Header("UIToolkit Options (このバックエンド固有の設定)")]
        [SerializeField] private UIToolkitOptions _uiToolkitOptions = new();

        private const string RootGameObjectName = "Terminal UIToolkit Root";

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

        [NonSerialized] private ColorPaletteAccessor _colorPaletteAccessor;
        [NonSerialized] private WindowAnimationAccessor _windowAnimationAccessor;
        [NonSerialized] private LauncherVisibleAccessor _launcherVisibleAccessor;
        [NonSerialized] private IPromptRenderer _promptRenderer;
        [NonSerialized] private LogRenderer _logRenderer;
        [NonSerialized] private NormalMode _normalMode;
        [NonSerialized] private GameObject _rootGameObject;
        [NonSerialized] private WindowRoot _windowRoot;
        [NonSerialized] private CursorFlashSpeedAccessor _cursorFlashSpeedAccessor;

        #endregion

        TerminalRuntimeScope IInstaller.Install()
        {
            var theme = _theme ?? new TerminalNullTheme();
            var animation = _animation ?? new TerminalNullAnimation();
            var options = _options ?? new TerminalNullOptions();

            DomainContext domainContext = default;
            RenderingContext renderingContext = default;
            CoordinatorContext coordinatorContext = default;

            try
            {
                domainContext = BuildDomainContext(options);
                RegisterCommands(in domainContext);
                renderingContext = BuildRenderingContext(theme, animation, options, in domainContext);
                coordinatorContext = BuildCoordinatorContext(in domainContext, in renderingContext, options);
                return BuildScope(in domainContext, in renderingContext, in coordinatorContext);
            }
            catch (Exception)
            {
                void CleanUp(IReadOnlyList<object> components)
                {
                    if (components == null) return;

                    for (var i = 0; i < components.Count; i++)
                    {
                        if (components[i] is IDisposable component)
                        {
                            component.Dispose();
                        }
                    }
                }

                CleanUp(domainContext.Components);
                CleanUp(renderingContext.Components);
                CleanUp(coordinatorContext.Components);
                ClearReferences();
                throw;
            }
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            try
            {
                (scope as IDisposable)?.Dispose();
            }
            finally
            {
                ClearReferences();
            }
        }

        async ValueTask IInstaller.UninstallAsync(TerminalRuntimeScope scope)
        {
            try
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    (scope as IDisposable)?.Dispose();
                }
            }
            finally
            {
                ClearReferences();
            }
        }

        void IInstaller.Resolve(TerminalRuntimeScope scope)
        {
            if (scope == null) return;

            var theme = _theme ?? new TerminalNullTheme();
            var animation = _animation ?? new TerminalNullAnimation();
            var options = _options ?? new TerminalNullOptions();

            SyncTheme(theme);
            SyncAnimation(animation);
            SyncOptions(options);
        }

        private void ClearReferences()
        {
            _colorPaletteAccessor = null;
            _windowAnimationAccessor = null;
            _launcherVisibleAccessor = null;
            _promptRenderer = null;
            _logRenderer = null;
            _normalMode = null;
            _rootGameObject = null;
            _windowRoot = null;
            _cursorFlashSpeedAccessor = null;
        }

        private void SyncTheme(ITerminalTheme theme)
        {
            if (_colorPaletteAccessor != null)
            {
                _colorPaletteAccessor[Definitions.ThemeLabel.Message] = theme.MessageColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Entry] = theme.EntryColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Warning] = theme.WarningColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Error] = theme.ErrorColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Assert] = theme.AssertColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Exception] = theme.ExceptionColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.System] = theme.SystemColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Cursor] = theme.CaretColor;
                _colorPaletteAccessor[Definitions.ThemeLabel.Selection] = theme.SelectionColor;
            }

            if (_cursorFlashSpeedAccessor != null)
            {
                _cursorFlashSpeedAccessor.FlashSpeed = theme.CursorFlashSpeed;
            }

            if (_logRenderer != null)
            {
                _logRenderer.CopyButtonColor = theme.CopyButtonColor;
            }

            ApplyThemeColors(theme);
        }

        private void SyncAnimation(ITerminalAnimation animation)
        {
            if (_windowAnimationAccessor == null) return;

            _windowAnimationAccessor.Anchor = animation.Anchor;
            _windowAnimationAccessor.Style = animation.WindowStyle;
            _windowAnimationAccessor.Duration = UIToolkitWindowAnimationDuration;
            _windowAnimationAccessor.Scale = animation.CompactScale;
        }

        private void SyncOptions(ITerminalOptions options)
        {
            if (_launcherVisibleAccessor != null)
            {
                _launcherVisibleAccessor.IsVisible = options.IsButtonVisible;
                _launcherVisibleAccessor.IsReverse = options.IsButtonReverse;
            }

            if (_normalMode != null)
            {
                _normalMode.Prompt = options.Prompt;
            }

            if (_promptRenderer != null)
            {
                _promptRenderer.ShowLoadingIndicator = options.ShowLoadingIndicator;
                _promptRenderer.LoadingIndicatorFrames = options.LoadingIndicatorFrames;
            }
        }

        private InputKeyboardType ResolveKeyboardType(ITerminalOptions options)
        {
#if ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            return options.Input.InputKeyboardType;
#elif ENABLE_INPUT_SYSTEM
            return InputKeyboardType.InputSystem;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return InputKeyboardType.Legacy;
#else
            return InputKeyboardType.None;
#endif
        }

        private IKeyboardInputHandler CreateInputHandler(ITerminalOptions options, InputKeyboardType resultType)
        {
            var input = options.Input;
            var factory =
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(input.InputSystemKey, input.LegacyInputKey, input.TriggerTiming, input.Priority);
#elif ENABLE_INPUT_SYSTEM
                new TerminalKeyboardFactory(input.InputSystemKey, input.TriggerTiming, input.Priority);
#elif ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(input.LegacyInputKey, input.TriggerTiming, input.Priority);
#else
                new TerminalKeyboardFactory();
#endif
            return factory.Create(resultType);
        }

        private IWindowFocusInputGuard CreateWindowFocusInputGuard(ITerminalOptions options, InputKeyboardType resultType)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (resultType is InputKeyboardType.Legacy && options.Input.AllowKeyInputWhileTextFieldFocused)
            {
                return new LegacyTextFieldKeyEatingGuard();
            }
#endif
            return NullWindowFocusInputGuard.Instance;
        }

        private DomainContext BuildDomainContext(ITerminalOptions options)
        {
            var logger = new CommandLogger(options.BufferSize);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var discover = new CommandDiscoverer(logger, new[] { "Assembly-CSharp" }.Concat(options.AdditionalCommandAssemblies ?? Array.Empty<string>()));
            var autocomplete = new CommandAutocomplete();
            var normalMode = new NormalMode(logger, registry, invoker, parser, history, autocomplete) { Prompt = options.Prompt };
            _normalMode = normalMode;
            var modeCommandBinder = new ModeCommandBinder(discover, () => new CommandRegistry(logger), logger);
            var executeCommandUseCase = new ExecuteCommandUseCase(logger, normalMode, modeCommandBinder);
            var service = new TerminalService(
                logger,
                registry,
                autocomplete,
                executeCommandUseCase
            );

            return new DomainContext
            {
                Components = new object[] { logger, registry, history, autocomplete, discover, executeCommandUseCase, service },
                Logger = logger,
                Registry = registry,
                History = history,
                Autocomplete = autocomplete,
                Discoverer = discover,
                Service = service,
                UseCase = executeCommandUseCase,
            };
        }

        private void RegisterCommands(in DomainContext domain)
        {
            var services = new Dictionary<Type, object>
            {
                { typeof(IModeStackInspector), domain.UseCase },
                { typeof(IModeOutput), domain.UseCase.Output },
                { typeof(IModeTransitionRequestSink), domain.UseCase.Transitions },
                { typeof(ICommandRegistry), domain.Registry },
                { typeof(ICommandLogger), domain.Logger },
            };
            var bundle = new ModeServiceBundle(services);

            var specs = domain.Discoverer.Discover();
            foreach (var spec in specs)
            {
                var handler = CommandFactory.Create(spec.Method, bundle);
                if (domain.Registry.Add(spec.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(spec.Meta.Command);
                }
            }

            RegisterBuiltinCommands(domain, bundle);
        }

        private void RegisterBuiltinCommands(in DomainContext domain, in ModeServiceBundle bundle)
        {
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinDiagnosticsCommands.Methods);
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinGeneralCommands.Methods);

#if UNITY_EDITOR
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinEditorCommands.Methods);
#endif
        }

        private static void RegisterBuiltinCommandMethods(in DomainContext domain, in ModeServiceBundle bundle, MethodInfo[] methods)
        {
            foreach (var method in methods)
            {
                var handler = CommandFactory.Create(method, bundle);
                if (domain.Registry.Add(handler.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(handler.Meta.Command);
                }
            }
        }

        /// <summary>
        /// <see cref="WindowAnimator"/>はOpen/Close遷移中、アンカーに応じて位置(X/Y)を毎フレーム
        /// 徐々に変化させるスライド方式で動く。IMGUIは毎フレーム再描画するimmediate-modeのため
        /// 問題にならないが、UIToolkitでは複数フレームにわたって要素が画面内へ徐々に移動してくる
        /// 過程で、ランタイム側のクリップ領域キャッシュが不整合を起こし、アニメーション完了後も
        /// 一部領域(スクロール可能な子要素を持つ場合の兄弟要素など)が描画されなくなる不具合を
        /// 実機検証で確認した(#122)。<see cref="WindowRoot.ApplyRect"/>側でstyle.translateに
        /// 逃がしても同様に再現したため、根本原因は「レイアウト vs GPU transform」ではなく
        /// 「複数フレームにわたる漸次的な位置変化」自体にあると判断し、UIToolkit版では
        /// Duration=0(瞬時に開閉、スライド演出なし)に固定して回避する。
        /// </summary>
        private const float UIToolkitWindowAnimationDuration = 0f;

        private RenderingContext BuildRenderingContext(ITerminalTheme theme, ITerminalAnimation animation, ITerminalOptions options, in DomainContext domain)
        {
            _windowAnimationAccessor = new WindowAnimationAccessor
            {
                State = animation.BootupWindowState,
                Anchor = animation.Anchor,
                Style = animation.WindowStyle,
                Duration = UIToolkitWindowAnimationDuration,
                Scale = animation.CompactScale,
            };

            _colorPaletteAccessor = new ColorPaletteAccessor(new Dictionary<string, Color>
            {
                { Definitions.ThemeLabel.Message, theme.MessageColor },
                { Definitions.ThemeLabel.Entry, theme.EntryColor },
                { Definitions.ThemeLabel.Warning, theme.WarningColor },
                { Definitions.ThemeLabel.Error, theme.ErrorColor },
                { Definitions.ThemeLabel.Assert, theme.AssertColor },
                { Definitions.ThemeLabel.Exception, theme.ExceptionColor },
                { Definitions.ThemeLabel.System, theme.SystemColor },
                { Definitions.ThemeLabel.Cursor, theme.CaretColor },
                { Definitions.ThemeLabel.Selection, theme.SelectionColor },
            });

            var scrollAccessor = new ScrollAccessor();

            _launcherVisibleAccessor = new LauncherVisibleAccessor
            {
                IsVisible = options.IsButtonVisible,
                IsReverse = options.IsButtonReverse,
            };

            var (visualTreeAsset, styleSheet, panelSettings) = ResolveUIToolkitAssets();

            _rootGameObject = new GameObject(RootGameObjectName);
            var document = _rootGameObject.AddComponent<UIDocument>();
            document.visualTreeAsset = visualTreeAsset;
            document.panelSettings = panelSettings;

            _windowRoot = _rootGameObject.AddComponent<WindowRoot>();
            _windowRoot.Initialize(document);

            if (styleSheet != null && _windowRoot.Root != null)
            {
                _windowRoot.Root.styleSheets.Add(styleSheet);
            }

            var cursorView = new CursorView();
            var clipboardRenderer = new ClipboardRenderer(_launcherVisibleAccessor);
            var logRenderer = new LogRenderer(_windowRoot.LogScrollView, clipboardRenderer, _colorPaletteAccessor, _launcherVisibleAccessor, theme.CopyButtonColor);
            _logRenderer = logRenderer;

            // _logRenderer代入後に呼ぶ(ApplyThemeColors内でLogRendererのフォントも同期するため).
            ApplyThemeColors(theme);
            var inputRenderer = new InputRenderer(_windowRoot.InputField, scrollAccessor, cursorView);
            _promptRenderer = new PromptRenderer(_windowRoot.PromptLabel, domain.Service)
            {
                ShowLoadingIndicator = options.ShowLoadingIndicator,
                LoadingIndicatorFrames = options.LoadingIndicatorFrames,
            };
            var submitRenderer = new SubmitRenderer(_windowRoot.SubmitButton);
            var launcherRenderer = new LauncherRenderer(_windowRoot.LauncherContainer, _windowRoot.LauncherOpenButton, _windowRoot.LauncherCloseButton);

            var windowPresenter = new WindowPresenter(
                _windowAnimationAccessor,
                new WindowAnimator(),
                new ScreenSizeAccessor(),
                new UnityExceptionLogger());
            _cursorFlashSpeedAccessor = new CursorFlashSpeedAccessor(theme.CursorFlashSpeed);
            var cursorPresenter = new CursorPresenter(_cursorFlashSpeedAccessor, cursorView);
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var submitPresenter = new SubmitPresenter(submitRenderer, _launcherVisibleAccessor);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, _launcherVisibleAccessor, _windowAnimationAccessor);

            var view = new UIToolkitCoordinator(
                _windowRoot,
                logRenderer,
                inputRenderer,
                _promptRenderer,
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
                WindowAnimationAccessor = _windowAnimationAccessor,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                LogPresenter = logPresenter,
                SubmitPresenter = submitPresenter,
                LauncherPresenter = launcherPresenter,
                View = terminalView,

                Components = new object[]
                {
                    _windowAnimationAccessor,
                    _colorPaletteAccessor,
                    scrollAccessor,
                    _launcherVisibleAccessor,
                    _cursorFlashSpeedAccessor,

                    _windowRoot,
                    cursorView,
                    clipboardRenderer,
                    logRenderer,
                    inputRenderer,
                    _promptRenderer,
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
                },
            };
        }

        /// <summary>
        /// ImmediateModeInstaller(IMGUI)がGUIStyle経由で適用しているテーマ色を、
        /// UIToolkitのVisualElementへ直接適用する(<see cref="ITerminalTheme"/>のうち
        /// ログ色以外の背景・プロンプト・入力欄・各種ボタン色).
        /// </summary>
        /// <summary>
        /// <see cref="ITerminalTheme.FontSize"/>はIMGUI版の<c>GUIStyle.fontSize</c>向けに調整された値
        /// (既定55)であり、UIToolkitの<c>style.fontSize</c>(素のCSSピクセル相当)にそのまま渡すと
        /// 極端に巨大化する(#122で判明。両バックエンドで「同じ数値=同じ見た目」という前提自体が誤りだった)。
        /// フォント"種類"(<see cref="ITerminalTheme.Font"/>)は両バックエンドで共有して問題ないが、
        /// サイズはUIToolkit独自の既定値を使う.
        /// </summary>
        private const int UIToolkitFontSize = 14;

        private void ApplyThemeColors(ITerminalTheme theme)
        {
            if (_windowRoot == null || !_windowRoot.IsInitialized) return;

            var fontDefinition = ResolveFontDefinition(theme);

            if (_windowRoot.Root != null) _windowRoot.Root.style.backgroundColor = theme.BackgroundColor;

            // ScrollViewの内部クリッピングが、下に配置された兄弟要素(入力欄の行)における
            // 親(Root)自身の背景描画を阻害する現象を確認した(#122調査。resolvedStyle上は
            // 正しい値なのに実描画だけ欠落する。ScrollViewを隠すと直る再現性から、原因は
            // ScrollView側にあると判断)。親の描画に依存せず自己完結するよう、入力欄の行
            // 自体にも同じ背景色を明示的に持たせることで回避する.
            if (_windowRoot.InputRow != null) _windowRoot.InputRow.style.backgroundColor = theme.BackgroundColor;
            ApplyTextElementStyle(_windowRoot.PromptLabel, theme.PromptColor, fontDefinition, UIToolkitFontSize);
            ApplyInputFieldColors(theme);
            ApplyTextElementStyle(_windowRoot.InputField, null, fontDefinition, UIToolkitFontSize);
            ApplyTextElementStyle(_windowRoot.SubmitButton, theme.ExecuteButtonColor, fontDefinition, UIToolkitFontSize);
            ApplyTextElementStyle(_windowRoot.LauncherOpenButton, theme.ButtonColor, fontDefinition, UIToolkitFontSize);
            ApplyTextElementStyle(_windowRoot.LauncherCloseButton, theme.ButtonColor, fontDefinition, UIToolkitFontSize);

            if (_logRenderer != null)
            {
                _logRenderer.FontDefinition = fontDefinition;
                _logRenderer.FontSize = UIToolkitFontSize;
            }

            ApplyUIToolkitOptions();
        }

        /// <summary>
        /// <see cref="UIToolkitOptions"/>(このバックエンド固有、Inspectorから調整可能)を
        /// 実際のUIToolkit要素に反映する.
        /// </summary>
        private void ApplyUIToolkitOptions()
        {
            if (_windowRoot?.LogScrollView == null) return;

            var options = _uiToolkitOptions ?? new UIToolkitOptions();
            _windowRoot.LogScrollView.mouseWheelScrollSize = options.ScrollSensitivity;
            _windowRoot.LogScrollView.scrollDecelerationRate = options.ScrollDecelerationRate;
        }

        /// <summary>
        /// テーマにFontSizeを設定しても、実際のフォント(<see cref="FontDefinition"/>)が
        /// どのVisualElementにも割り当たっていないと、UIToolkitはグリフの計測ができず
        /// テキストの高さが常に0になる(色・fontSizeは正しく解決されるのに文字が一切
        /// 表示されない不具合として#122で判明)。<see cref="ITerminalTheme.Font"/>未指定時は
        /// Resourcesに頼らずUnity組み込みのArialへフォールバックする.
        /// </summary>
        private static FontDefinition ResolveFontDefinition(ITerminalTheme theme)
        {
            var font = theme.Font != null ? theme.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return FontDefinition.FromFont(font);
        }

        private static void ApplyTextElementStyle(VisualElement element, Color? color, FontDefinition fontDefinition, int fontSize)
        {
            if (element == null) return;

            if (color.HasValue) element.style.color = color.Value;
            element.style.unityFontDefinition = fontDefinition;
            element.style.fontSize = fontSize;
        }

        /// <summary>
        /// <see cref="TextField"/>は既定テーマ(unity-theme://default)の標準スキンにより
        /// 白背景の入力ボックスとして描画される。文字色だけでなく、外側の<see cref="TextField"/>と
        /// 内側の<c>unity-text-input</c>(<see cref="TextField.TextInput"/>)双方の背景・枠線も
        /// テーマ色で塗りつぶし、IMGUI版と印象を揃える.
        /// </summary>
        private void ApplyInputFieldColors(ITerminalTheme theme)
        {
            var field = _windowRoot.InputField;
            if (field == null) return;

            field.style.color = theme.InputColor;

            ApplyFieldBoxColors(field, theme.BackgroundColor);
            var textInput = field.Q(TextField.textInputUssName);
            if (textInput != null) ApplyFieldBoxColors(textInput, theme.BackgroundColor);
        }

        private static void ApplyFieldBoxColors(VisualElement element, Color backgroundColor)
        {
            element.style.backgroundColor = backgroundColor;
            element.style.borderTopColor = backgroundColor;
            element.style.borderBottomColor = backgroundColor;
            element.style.borderLeftColor = backgroundColor;
            element.style.borderRightColor = backgroundColor;
        }

        /// <summary>
        /// UXML/USS/PanelSettingsをInspectorの明示指定から解決する.
        /// </summary>
        /// <remarks>
        /// <c>Resources.Load</c>によるフォールバックは行わない。UIToolkitバックエンドのコード
        /// (Sample「UI Backend: UIToolkit」)とデフォルトアセット(Sample「UI Backend: UIToolkit
        /// Default Resources」)は別々に任意インポートされるため、Resources経由のフォールバックは
        /// 後者を未インポートのままだと機能しない暗黙依存になってしまう(#122で判明)。
        /// 未指定の場合は例外にせず、警告ログのうえ<see cref="WindowRoot.Initialize"/>側で
        /// コードのみによる最小限のフォールバックUIを構築する(#129 item1の「例外にせず警告+
        /// フォールバック」方針はResources非依存の形で維持する).
        /// </remarks>
        private (VisualTreeAsset visualTreeAsset, StyleSheet styleSheet, PanelSettings panelSettings) ResolveUIToolkitAssets()
        {
            if (_visualTreeAsset == null)
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] UIToolkit用のVisualTreeAssetが未指定です。" +
                    "コードのみで構築した最小限のフォールバックUIを使用します。");
            }

            var panelSettings = _panelSettings;
            if (panelSettings == null)
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] UIToolkit用のPanelSettingsが未指定です。" +
                    "実行時生成のPanelSettingsで代替します。");
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

                // 既定値はConstantPhysicalSize(参照DPIに対する実画面DPIの比率で拡大縮小される)。
                // IMGUI版はDPIスケーリングを一切行わないため、テーマのFontSize等をそのまま
                // 共有すると環境のDPIによって表示サイズが大きく食い違う(#122で判明。Retina等の
                // 高DPI環境でテキストが異常に巨大化する形で顕在化した)。IMGUIと同じ「1px=1px」の
                // 挙動に揃えるため、ピクセル等倍を明示する.
                panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            }

            return (_visualTreeAsset, _styleSheet, panelSettings);
        }

        private CoordinatorContext BuildCoordinatorContext(
            in DomainContext domain,
            in RenderingContext rendering,
            ITerminalOptions options)
        {
            var keyboardType = ResolveKeyboardType(options);
            var inputHandler = CreateInputHandler(options, keyboardType);
            var eventListener = new EventListener(inputHandler);
            var windowFocusInputGuard = CreateWindowFocusInputGuard(options, keyboardType);

            var coordinator = new TerminalCoordinator(
                domain.Service,
                rendering.GUI,
                rendering.ScrollMutator,
                rendering.WindowAnimationAccessor,
                rendering.WindowPresenter,
                rendering.InputPresenter,
                rendering.LogPresenter,
                rendering.SubmitPresenter,
                rendering.LauncherPresenter,
                eventListener,
                windowFocusInputGuard);

            return new CoordinatorContext
            {
                Coordinator = coordinator,
                EventListener = eventListener,
                Components = new object[]
                {
                    coordinator,
                    eventListener,
                }
            };
        }

        private TerminalRuntimeScope BuildScope(
            in DomainContext domain,
            in RenderingContext rendering,
            in CoordinatorContext coordinator)
        {
            var instances =
                domain.Components
                    .Concat(rendering.Components)
                    .Concat(coordinator.Components).ToArray();

            var updatables = instances.OfType<IUpdatable>().ToList();
            var asyncDisposables = instances.OfType<IAsyncDisposable>().ToList();
            var disposables = instances.OfType<IDisposable>().Where(d => d is not IAsyncDisposable).ToList();

            var entryPoint = new TerminalEntryPoint(updatables, rendering.GUI);

            return new TerminalRuntimeScope(
                entryPoint,
                domain.Service,
                domain.Registry,
                domain.Autocomplete,
                rendering.View,
                disposables,
                asyncDisposables,
                domain.Logger);
        }
    }
}
#endif

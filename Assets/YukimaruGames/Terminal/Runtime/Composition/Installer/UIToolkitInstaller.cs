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
using YukimaruGames.Terminal.Composition.Shared.Extensions;

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

        [Header("UIToolkit Assets (未指定時はResourcesへフォールバック)")]
        [SerializeField] private VisualTreeAsset _visualTreeAsset;
        [SerializeField] private StyleSheet _styleSheet;
        [SerializeField] private PanelSettings _panelSettings;

        private const string VisualTreeAssetResourcePath = "Terminal/UIToolKit/TerminalWindow";
        private const string StyleSheetResourcePath = "Terminal/UIToolKit/TerminalWindow";
        private const string PanelSettingsResourcePath = "Terminal/UIToolKit/TerminalPanelSettings";
        private const string RootGameObjectName = "Terminal UIToolkit Root";

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
            _windowAnimationAccessor.Duration = animation.Duration;
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

        private RenderingContext BuildRenderingContext(ITerminalTheme theme, ITerminalAnimation animation, ITerminalOptions options, in DomainContext domain)
        {
            _windowAnimationAccessor = new WindowAnimationAccessor
            {
                State = animation.BootupWindowState,
                Anchor = animation.Anchor,
                Style = animation.WindowStyle,
                Duration = animation.Duration,
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

            ApplyThemeColors(theme);

            var cursorView = new CursorView();
            var clipboardRenderer = new ClipboardRenderer(_launcherVisibleAccessor);
            var logRenderer = new LogRenderer(_windowRoot.LogScrollView, clipboardRenderer, _colorPaletteAccessor, _launcherVisibleAccessor, theme.CopyButtonColor);
            _logRenderer = logRenderer;
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
        private void ApplyThemeColors(ITerminalTheme theme)
        {
            if (_windowRoot == null || !_windowRoot.IsInitialized) return;

            if (_windowRoot.Root != null) _windowRoot.Root.style.backgroundColor = theme.BackgroundColor;
            if (_windowRoot.PromptLabel != null) _windowRoot.PromptLabel.style.color = theme.PromptColor;
            ApplyInputFieldColors(theme);
            if (_windowRoot.SubmitButton != null) _windowRoot.SubmitButton.style.color = theme.ExecuteButtonColor;
            if (_windowRoot.LauncherOpenButton != null) _windowRoot.LauncherOpenButton.style.color = theme.ButtonColor;
            if (_windowRoot.LauncherCloseButton != null) _windowRoot.LauncherCloseButton.style.color = theme.ButtonColor;
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
        /// UXML/USS/PanelSettingsを明示指定 → Resourcesフォールバックの順で解決する.
        /// いずれも解決できない場合は例外にせず、ログ警告のうえ最小限のフォールバックを構築する.
        /// </summary>
        private (VisualTreeAsset visualTreeAsset, StyleSheet styleSheet, PanelSettings panelSettings) ResolveUIToolkitAssets()
        {
            var visualTreeAsset = _visualTreeAsset.OrResource(VisualTreeAssetResourcePath);
            var styleSheet = _styleSheet.OrResource(StyleSheetResourcePath);
            var panelSettings = _panelSettings.OrResource(PanelSettingsResourcePath);

            if (visualTreeAsset == null)
            {
                Debug.LogWarning(
                    $"[YukimaruGames.Terminal] UIToolkit用のVisualTreeAssetが見つかりませんでした" +
                    $"(未指定 かつ Resources/{VisualTreeAssetResourcePath} も未検出)。最小限のフォールバックUIを生成します。");
            }

            if (panelSettings == null)
            {
                Debug.LogWarning(
                    $"[YukimaruGames.Terminal] UIToolkit用のPanelSettingsが見つかりませんでした" +
                    $"(未指定 かつ Resources/{PanelSettingsResourcePath} も未検出)。実行時生成のPanelSettingsで代替します。");
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            }

            return (visualTreeAsset, styleSheet, panelSettings);
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

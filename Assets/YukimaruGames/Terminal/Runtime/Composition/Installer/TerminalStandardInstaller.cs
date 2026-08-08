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
using YukimaruGames.Terminal.Adapters.GUI;
using YukimaruGames.Terminal.Adapters.GUI.Accessors;
using YukimaruGames.Terminal.Adapters.GUI.Renderers;
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
using YukimaruGames.Terminal.Infrastructure.Repositories;
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
using YukimaruGames.Terminal.Presentation.Interfaces.Repositories;
using YukimaruGames.Terminal.Presentation.Models;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    [Serializable]
    public sealed class TerminalStandardInstaller : IInstaller
    {
        #region inner-struct

        /// <summary>
        /// ドメイン層のパラメータをとりまとめたContext
        /// </summary>
        private struct DomainContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components;
            
            /// <inheritdoc cref="ITerminalService"/> 
            public ITerminalService Service;
            /// <inheritdoc cref="ICommandLogger"/>
            public ICommandLogger Logger;
            /// <inheritdoc cref="ICommandHistory"/>
            public ICommandHistory History;
            /// <inheritdoc cref="ICommandRegistry"/>
            public ICommandRegistry Registry;
            /// <inheritdoc cref="ICommandAutocomplete"/>
            public ICommandAutocomplete Autocomplete;
            /// <inheritdoc cref="ICommandDiscoverer"/>
            public ICommandDiscoverer Discoverer;
            /// <inheritdoc cref="IExecuteCommandUseCase"/>
            public IExecuteCommandUseCase UseCase;
        }

        /// <summary>
        /// プレゼンテーション層のパラメータをとりまとめたContext
        /// </summary>
        private struct RenderingContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components;
            
            /// <inheritdoc cref="ITerminalGUI"/> 
            public ITerminalGUI GUI;
            /// <inheritdoc cref="IScrollMutator"/>
            public IScrollMutator ScrollMutator;
            /// <inheritdoc cref="IWindowAnimationAccessor"/>
            public IWindowAnimationAccessor WindowAnimationAccessor;
            /// <inheritdoc cref="IWindowPresenter"/>
            public IWindowPresenter WindowPresenter;
            /// <inheritdoc cref="IInputPresenter"/>
            public IInputPresenter InputPresenter;
            /// <inheritdoc cref="ILogPresenter"/>
            public ILogPresenter LogPresenter;
            /// <inheritdoc cref="ISubmitPresenter"/>
            public ISubmitPresenter SubmitPresenter;
            /// <inheritdoc cref="ILauncherPresenter"/>
            public ILauncherPresenter LauncherPresenter;
            /// <inheritdoc cref="ITerminalView"/>
            public ITerminalView View { get; set; }
        }
        
        /// <summary>
        /// orchestratorをとりまとめたコンテキスト
        /// </summary>
        private struct CoordinatorContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components;
            
            /// <inheritdoc cref="Coordinator"/>
            public TerminalCoordinator Coordinator;
            /// <inheritdoc cref="IEventListener"/>
            public IEventListener EventListener;
        }

        #endregion
        
        [SerializeReference, SerializeInterface] 
        private ITerminalTheme _theme = new TerminalStandardTheme();

        [SerializeReference, SerializeInterface] 
        private ITerminalAnimation _animation = new TerminalStandardAnimation();

        [SerializeReference, SerializeInterface] 
        private ITerminalOptions _options = new TerminalStandardOptions();

        #region runtime-instances

        [NonSerialized] private FontAccessor _fontAccessor;
        [NonSerialized] private ColorPaletteAccessor _colorPaletteAccessor;
        [NonSerialized] private WindowAnimationAccessor _windowAnimationAccessor;
        [NonSerialized] private LauncherVisibleAccessor _launcherVisibleAccessor;
        [NonSerialized] private IWindowRenderer _windowRenderer;
        [NonSerialized] private IPromptRenderer _promptRenderer;
        [NonSerialized] private NormalMode _normalMode;
        [NonSerialized] private CursorFlashSpeedAccessor _cursorFlashSpeedAccessor;
        
        [NonSerialized] private IGUIStyleAccessor _logGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _inputGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _promptGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _executeButtonsGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _launcherGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _logCopyButtonGUIStyleAccessor;

        [NonSerialized] private IPixelTextureRepository _pixelTextureRepository;
        #endregion

        TerminalRuntimeScope IInstaller.Install()
        {
            // Null Object Pattern: 意図的な null は Null 実装にフォールバック
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
                    if (components == null)
                    {
                        return;
                    }

                    // Interface 越しの foreach による GC Alloc を避けるため、for で列挙
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
            _fontAccessor = null;
            _colorPaletteAccessor = null;
            _windowAnimationAccessor = null;
            _launcherVisibleAccessor = null;
            _windowRenderer = null;
            _promptRenderer = null;
            _normalMode = null;
            _cursorFlashSpeedAccessor = null;
            _logGUIStyleAccessor = null;
            _inputGUIStyleAccessor = null;
            _promptGUIStyleAccessor = null;
            _executeButtonsGUIStyleAccessor = null;
            _launcherGUIStyleAccessor = null;
            _logCopyButtonGUIStyleAccessor = null;
            _pixelTextureRepository = null;
        }

        private void SyncTheme(ITerminalTheme theme)
        {
            if (_fontAccessor != null)
            {
                _fontAccessor.Font = theme.Font;
                _fontAccessor.Size = theme.FontSize;
            }
            
            _inputGUIStyleAccessor?.SetColor(theme.InputColor);
            _promptGUIStyleAccessor?.SetColor(theme.PromptColor);
            _executeButtonsGUIStyleAccessor?.SetColor(theme.ExecuteButtonColor);
            _launcherGUIStyleAccessor?.SetColor(theme.ButtonColor);
            _logCopyButtonGUIStyleAccessor?.SetColor(theme.CopyButtonColor);

            if (_cursorFlashSpeedAccessor != null)
            {
                _cursorFlashSpeedAccessor.FlashSpeed = theme.CursorFlashSpeed;
            }

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

            _pixelTextureRepository?.SetColor(Definitions.ThemeLabel.Window, theme.BackgroundColor);
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
            // static コマンドから ITerminalModeStack を注入可能にする(python等の入場コマンド、
            // terminal.stack 等の診断コマンド用)。ITerminalService丸ごとは注入しない
            // (ExecuteAsync等を誤って呼ぶとディスパッチャの排他ロックでデッドロックするため).
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

            // terminal.stack等のパッケージ内蔵コマンドは、Assembly-CSharpの参照グラフ次第で
            // 属性発見(ICommandDiscoverer.Discover)に乗らない場合がある(利用者コードが実際に
            // 型を参照していないアセンブリはAssemblyRefに現れないため)。Composition層は
            // Infrastructureを直接知っているので、確実性のためここで直接登録する.
            RegisterBuiltinCommands(domain, bundle);
        }

        private void RegisterBuiltinCommands(in DomainContext domain, in ModeServiceBundle bundle)
        {
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinDiagnosticsCommands.Methods);
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinGeneralCommands.Methods);

#if UNITY_EDITOR
            // Editor限定コマンドは実機ビルド(UNITY_EDITOR未定義)では型ごとコンパイル対象外になる
            // ため、この呼び出し自体も#if UNITY_EDITORで囲い、実機ビルドに参照を残さない.
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
            _windowAnimationAccessor = new WindowAnimationAccessor()
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

            _fontAccessor = new FontAccessor(theme.Font) { Size = theme.FontSize };
            _pixelTextureRepository = new PixelTextureRepository();
            var scrollAccessor = new ScrollAccessor();

            // Style contexts
            _logGUIStyleAccessor = new GUIStyleAccessor(_fontAccessor);
            _inputGUIStyleAccessor = new GUIStyleAccessor(_fontAccessor);
            _promptGUIStyleAccessor = new GUIStyleAccessor(_fontAccessor);
            _executeButtonsGUIStyleAccessor = new GUIStyleAccessor(_fontAccessor);
            _launcherGUIStyleAccessor = new GUIStyleAccessor(_fontAccessor);
            _logCopyButtonGUIStyleAccessor = new GUIStyleAccessor(_fontAccessor);

            // Apply Colors immediately
            SyncTheme(theme);

            _cursorFlashSpeedAccessor = new CursorFlashSpeedAccessor(theme.CursorFlashSpeed);
            _launcherVisibleAccessor = new LauncherVisibleAccessor
            {
                IsVisible = options.IsButtonVisible,
                IsReverse = options.IsButtonReverse,
            };

            // Renderers
            _windowRenderer = new WindowRenderer(_pixelTextureRepository);

            var cursorView = new CursorView();
            var logLinePool = new LogLinePool();
            var clipboardRenderer = new ClipboardRenderer(_launcherVisibleAccessor, _logCopyButtonGUIStyleAccessor);
            var logRenderer = new LogRenderer(clipboardRenderer, _logGUIStyleAccessor, _colorPaletteAccessor, logLinePool);
            var inputRenderer = new InputRenderer(scrollAccessor, _inputGUIStyleAccessor, _colorPaletteAccessor, cursorView);
            _promptRenderer = new PromptRenderer(_promptGUIStyleAccessor, domain.Service)
            {
                ShowLoadingIndicator = options.ShowLoadingIndicator,
                LoadingIndicatorFrames = options.LoadingIndicatorFrames,
            };
            var executeButtonRenderer = new SubmitRenderer(_executeButtonsGUIStyleAccessor);
            var launcherRenderer = new LauncherRenderer(_pixelTextureRepository, _launcherGUIStyleAccessor);

            // Presenters
            var windowPresenter = new WindowPresenter(
                _windowAnimationAccessor,
                new WindowAnimator(),
                new ScreenSizeAccessor(),
                new UnityExceptionLogger());
            var cursorPresenter = new CursorPresenter(_cursorFlashSpeedAccessor, cursorView);
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var executeButtonPresenter = new SubmitPresenter(executeButtonRenderer, _launcherVisibleAccessor);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, _launcherVisibleAccessor, _windowAnimationAccessor);

            // View
            var viewContext = new ViewContext
            {
                WindowRenderer = _windowRenderer,
                ClipboardRenderer = clipboardRenderer,
                LogRenderer = logRenderer,
                InputRenderer = inputRenderer,
                PromptRenderer = _promptRenderer,
                SubmitRenderer = executeButtonRenderer,
                LauncherRenderer = launcherRenderer,

                WindowRenderDataProvider = windowPresenter,
                LogRenderDataProvider = logPresenter,
                InputRenderDataProvider = inputPresenter,
                SubmitRenderDataProvider = executeButtonPresenter,
                LauncherRenderDataProvider = launcherPresenter,

                ScrollAccessor = scrollAccessor,
            };
            var view = new TerminalIMGUI(viewContext);
            var terminalView = new TerminalView(windowPresenter);

            return new RenderingContext
            {
                GUI = view,
                ScrollMutator = scrollAccessor,
                WindowAnimationAccessor = _windowAnimationAccessor,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                LogPresenter = logPresenter,
                SubmitPresenter = executeButtonPresenter,
                LauncherPresenter = launcherPresenter,
                View = terminalView,

                Components = new object[]
                {
                    _windowAnimationAccessor,
                    _colorPaletteAccessor,
                    _fontAccessor,
                    _pixelTextureRepository,
                    scrollAccessor,

                    _logGUIStyleAccessor,
                    _inputGUIStyleAccessor,
                    _promptGUIStyleAccessor,
                    _executeButtonsGUIStyleAccessor,
                    _launcherGUIStyleAccessor,
                    _logCopyButtonGUIStyleAccessor,

                    _cursorFlashSpeedAccessor,
                    _launcherVisibleAccessor,

                    _windowRenderer,
                    cursorView,
                    logLinePool,
                    clipboardRenderer,
                    logRenderer,
                    inputRenderer,
                    _promptRenderer,
                    executeButtonRenderer,
                    launcherRenderer,

                    windowPresenter,
                    cursorPresenter,
                    logPresenter,
                    inputPresenter,
                    executeButtonPresenter,
                    launcherPresenter,

                    viewContext,
                    view,
                    terminalView,
                },
            };
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

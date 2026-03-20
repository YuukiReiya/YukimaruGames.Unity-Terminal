#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Runtime.Input.LegacyInput;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Service;
using YukimaruGames.Terminal.Infrastructure;
using YukimaruGames.Terminal.Infrastructure.Context;
using YukimaruGames.Terminal.Infrastructure.Discovery;
using YukimaruGames.Terminal.Infrastructure.Service;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.UI.Core;
using YukimaruGames.Terminal.UI.Input;
using YukimaruGames.Terminal.UI.Launcher;
using YukimaruGames.Terminal.UI.Log;
using YukimaruGames.Terminal.UI.Main;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable]
    public sealed class StandardInstaller : IInstaller
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
            
            /// <inheritdoc cref="IMainView"/> 
            public IMainView View;
            /// <inheritdoc cref="IScrollConfigurator"/>
            public IScrollConfigurator ScrollConfigurator;
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
            
            /// <inheritdoc cref="UI.Main.Coordinator"/> 
            public Coordinator Coordinator;
            /// <inheritdoc cref="IEventListener"/> 
            public IEventListener EventListener;
            /// <summary>解決済みキーボード入力種別.</summary>
            public InputKeyboardType KeyboardType;
        }

        #endregion
        
        [SerializeReference, SerializeInterface] 
        private ITerminalTheme _theme = new TerminalStandardTheme();

        [SerializeReference, SerializeInterface] 
        private ITerminalAnimation _animation = new TerminalStandardAnimation();

        [SerializeReference, SerializeInterface] 
        private ITerminalOptions _options = new TerminalStandardOptions();

        TerminalRuntimeScope IInstaller.Install()
        {
            // Null Object Pattern: 意図的な null は Null 実装にフォールバック
            var theme = _theme ?? new TerminalNullTheme();
            var animation = _animation ?? new TerminalNullAnimation();
            var options = _options ?? new TerminalNullOptions();

            var domain = BuildDomainContext(options);
            RegisterCommands(in domain);
            var rendering = BuildRenderingContext(theme, animation, options, in domain);
            var coordinator = BuildCoordinatorContext(in domain, in rendering, options);

            return BuildScope(in domain, in rendering, in coordinator);
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            (scope as IDisposable)?.Dispose();
        }

        private InputKeyboardType ResolveKeyboardType(ITerminalOptions options)
        {
#if ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            return options.InputKeyboardType;
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
            var factory =
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(options.InputSystemKey, options.LegacyInputKey);
#elif ENABLE_INPUT_SYSTEM
                new TerminalKeyboardFactory(options.InputSystemKey);
#elif ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(options.LegacyInputKey);
            #else
                new TerminalKeyboardFactory();
            #endif
            return factory.Create(resultType);
        }

        private DomainContext BuildDomainContext(ITerminalOptions options)
        {
            var logger = new CommandLogger(options.BufferSize);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var discover = new CommandDiscoverer(logger);
            var autocomplete = new CommandAutocomplete();
            var service = new TerminalService(
                logger,
                registry,
                invoker,
                parser,
                history,
                autocomplete
            );

            return new DomainContext
            {
                Components = new object[] { logger, registry, history, autocomplete, discover, service },
                Logger = logger,
                Registry = registry,
                History = history,
                Autocomplete = autocomplete,
                Discoverer = discover,
                Service = service,
            };
        }

        private void RegisterCommands(in DomainContext domain)
        {
            var specs = domain.Discoverer.Discover();
            foreach (var spec in specs)
            {
                var handler = CommandFactory.Create(spec.Method);
                if (domain.Registry.Add(spec.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(spec.Meta.Command);
                }
            }
        }

        private RenderingContext BuildRenderingContext(ITerminalTheme theme, ITerminalAnimation animation, ITerminalOptions options, in DomainContext domain)
        {
            var animatorDataConfigurator = new WindowAnimatorDataConfigurator()
            {
                State = animation.BootupWindowState,
                Anchor = animation.Anchor,
                Style = animation.WindowStyle,
                Duration = animation.Duration,
                Scale = animation.CompactScale,
            };

            var colorPaletteProvider = new ColorPaletteProvider(new Dictionary<string, Color>
            {
                { Constants.ColorPalette.Message, theme.MessageColor },
                { Constants.ColorPalette.Entry, theme.EntryColor },
                { Constants.ColorPalette.Warning, theme.WarningColor },
                { Constants.ColorPalette.Error, theme.ErrorColor },
                { Constants.ColorPalette.Assert, theme.AssertColor },
                { Constants.ColorPalette.Exception, theme.ExceptionColor },
                { Constants.ColorPalette.System, theme.SystemColor },
                { Constants.ColorPalette.Cursor, theme.CaretColor },
                { Constants.ColorPalette.Selection, theme.SelectionColor },
            });

            var fontProvider = new FontProvider(theme.Font) { Size = theme.FontSize };
            var pixelTexture2DRepository = new PixelTextureRepository();
            var scrollConfigurator = new ScrollConfigurator();

            // Style contexts
            var logStyleContext = new StyleContext(fontProvider);
            var inputStyleContext = new StyleContext(fontProvider);
            var promptStyleContext = new StyleContext(fontProvider);
            var executeButtonsStyleContext = new StyleContext(fontProvider);
            var openButtonsStyleContext = new StyleContext(fontProvider);
            var logCopyButtonStyleContext = new StyleContext(fontProvider);

            // Apply Colors immediately
            inputStyleContext.SetColor(theme.InputColor);
            promptStyleContext.SetColor(theme.PromptColor);
            executeButtonsStyleContext.SetColor(theme.ExecuteButtonColor);
            openButtonsStyleContext.SetColor(theme.ButtonColor);
            logCopyButtonStyleContext.SetColor(theme.CopyButtonColor);

            var cursorFlashSpeedProvider = new CursorFlashSpeedProvider(theme.CursorFlashSpeed);
            var launcherVisibleConfigurator = new LauncherVisibleConfigurator
            {
                IsVisible = options.IsButtonVisible,
                IsReverse = options.IsButtonReverse,
            };

            // Renderers
            var windowRenderer = new WindowRenderer(pixelTexture2DRepository);
            windowRenderer.SetBackgroundColor(theme.BackgroundColor);
            var clipboardRenderer = new ClipboardRenderer(launcherVisibleConfigurator, logCopyButtonStyleContext);
            var logRenderer = new LogRenderer(clipboardRenderer, logStyleContext, colorPaletteProvider);
            var inputRenderer = new InputRenderer(scrollConfigurator, inputStyleContext, colorPaletteProvider, cursorFlashSpeedProvider);
            var promptRenderer = new PromptRenderer(promptStyleContext) { Prompt = options.Prompt };
            var executeButtonRenderer = new SubmitRenderer(executeButtonsStyleContext);
            var launcherRenderer = new LauncherRenderer(pixelTexture2DRepository, openButtonsStyleContext);

            // Presenters
            var windowPresenter = new WindowPresenter(animatorDataConfigurator, new WindowAnimator());
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var executeButtonPresenter = new SubmitPresenter(executeButtonRenderer, launcherVisibleConfigurator);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, launcherVisibleConfigurator);

            // View
            var viewContext = new ViewContext
            {
                WindowRenderer = windowRenderer,
                ClipboardRenderer = clipboardRenderer,
                LogRenderer = logRenderer,
                InputRenderer = inputRenderer,
                PromptRenderer = promptRenderer,
                SubmitRenderer = executeButtonRenderer,
                LauncherRenderer = launcherRenderer,

                WindowRenderDataProvider = windowPresenter,
                LogRenderDataProvider = logPresenter,
                InputRenderDataProvider = inputPresenter,
                SubmitRenderDataProvider = executeButtonPresenter,
                LauncherRenderDataProvider = launcherPresenter,

                ScrollConfigurator = scrollConfigurator,
            };
            var view = new MainView(viewContext);

            return new RenderingContext
            {
                View = view,
                ScrollConfigurator = scrollConfigurator,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                LogPresenter = logPresenter,
                SubmitPresenter = executeButtonPresenter,
                LauncherPresenter = launcherPresenter,
                
                Components = new object[]
                {
                    animatorDataConfigurator,
                    colorPaletteProvider,
                    fontProvider,
                    pixelTexture2DRepository,
                    scrollConfigurator,
                    
                    logStyleContext,
                    inputStyleContext,
                    promptStyleContext,
                    executeButtonsStyleContext,
                    openButtonsStyleContext,
                    logCopyButtonStyleContext,
                    
                    cursorFlashSpeedProvider,
                    launcherVisibleConfigurator,
                    
                    windowRenderer,
                    clipboardRenderer,
                    logRenderer,
                    inputRenderer,
                    promptRenderer,
                    executeButtonRenderer,
                    launcherRenderer,

                    windowPresenter,
                    logPresenter,
                    inputPresenter,
                    executeButtonPresenter,
                    launcherPresenter,
                    
                    viewContext,
                    view
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

            var coordinator = new Coordinator(
                domain.Service,
                rendering.View,
                rendering.ScrollConfigurator,
                rendering.WindowPresenter,
                rendering.InputPresenter,
                rendering.LogPresenter,
                rendering.SubmitPresenter,
                rendering.LauncherPresenter,
                eventListener);

            return new CoordinatorContext
            {
                Coordinator = coordinator,
                EventListener = eventListener,
                KeyboardType = keyboardType,
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
            var disposables = instances.OfType<IDisposable>().ToList();

            var entryPoint = new TerminalEntryPoint(updatables, coordinator.KeyboardType, rendering.View);

            return new TerminalRuntimeScope(
                entryPoint,
                domain.Service,
                domain.Registry,
                domain.Autocomplete,
                disposables);
        }
    }
}

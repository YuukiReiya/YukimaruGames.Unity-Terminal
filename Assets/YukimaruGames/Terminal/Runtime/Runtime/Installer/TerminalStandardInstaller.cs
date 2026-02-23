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
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;
using YukimaruGames.Terminal.UI.View.Input;
using ColorPalette = YukimaruGames.Terminal.SharedKernel.Constants.Constants.ColorPalette;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable]
    public class TerminalStandardInstaller : ITerminalInstaller
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
            /// <inheritdoc cref="CommandFactory"/>
            public CommandFactory Factory;
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
            
            /// <inheritdoc cref="ITerminalView"/> 
            public ITerminalView View;
            /// <inheritdoc cref="IScrollConfigurator"/>
            public IScrollConfigurator ScrollConfigurator;
            /// <inheritdoc cref="ITerminalWindowPresenter"/>
            public ITerminalWindowPresenter WindowPresenter;
            /// <inheritdoc cref="ITerminalInputPresenter"/>
            public ITerminalInputPresenter InputPresenter;
            /// <inheritdoc cref="ITerminalExecuteButtonPresenter"/>
            public ITerminalExecuteButtonPresenter ExecuteButtonPresenter;
            /// <inheritdoc cref="ITerminalButtonPresenter"/>
            public ITerminalButtonPresenter ButtonPresenter;
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
            
            /// <inheritdoc cref="TerminalCoordinator"/> 
            public TerminalCoordinator Coordinator;
            /// <inheritdoc cref="ITerminalEventListener"/> 
            public ITerminalEventListener EventListener;
            /// <summary>解決済みキーボード入力種別.</summary>
            public InputKeyboardType KeyboardType;
        }

        #endregion
        
        [SerializeReference, SerializeInterface] 
        private ITerminalTheme _theme = new TerminalStandardTheme();

        [SerializeReference, SerializeInterface] 
        private ITerminalOptions _options = new TerminalStandardOptions();

        TerminalRuntimeScope ITerminalInstaller.Install()
        {
            // Null Object Pattern: 意図的な null は Null 実装にフォールバック
            var theme = _theme ?? new TerminalNullTheme();
            var options = _options ?? new TerminalNullOptions();

            var domain = BuildDomainContext(options);
            RegisterCommands(in domain);
            var rendering = BuildRenderingContext(theme, options, in domain);
            var coordinator = BuildCoordinatorContext(in domain, in rendering, options);

            return BuildScope(in domain, in rendering, in coordinator);
        }

        void ITerminalInstaller.Uninstall(TerminalRuntimeScope scope)
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
            var factory = new CommandFactory();
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
                Components = new object[] { logger, registry, history, autocomplete, discover, factory, service },
                Logger = logger,
                Registry = registry,
                History = history,
                Autocomplete = autocomplete,
                Discoverer = discover,
                Factory = factory,
                Service = service,
            };
        }

        private void RegisterCommands(in DomainContext domain)
        {
            var specs = domain.Discoverer.Discover();
            foreach (var spec in specs)
            {
                var handler = domain.Factory.Create(spec.Method);
                if (domain.Registry.Add(spec.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(spec.Meta.Command);
                }
            }
        }

        private RenderingContext BuildRenderingContext(ITerminalTheme theme, ITerminalOptions options, in DomainContext domain)
        {
            var animatorDataConfigurator = new TerminalWindowAnimatorDataConfigurator()
            {
                State = theme.BootupWindowState,
                Anchor = theme.Anchor,
                Style = theme.WindowStyle,
                Duration = theme.Duration,
                Scale = theme.CompactScale,
            };

            var colorPaletteProvider = new ColorPaletteProvider(new Dictionary<string, Color>
            {
                { ColorPalette.Message, theme.MessageColor },
                { ColorPalette.Entry, theme.EntryColor },
                { ColorPalette.Warning, theme.WarningColor },
                { ColorPalette.Error, theme.ErrorColor },
                { ColorPalette.Assert, theme.AssertColor },
                { ColorPalette.Exception, theme.ExceptionColor },
                { ColorPalette.System, theme.SystemColor },
                { ColorPalette.Cursor, theme.CaretColor },
                { ColorPalette.Selection, theme.SelectionColor },
            });

            var fontProvider = new TerminalFontProvider(theme.Font) { Size = theme.FontSize };
            var pixelTexture2DRepository = new PixelTexture2DRepository();
            var scrollConfigurator = new ScrollConfigurator();

            // Style contexts
            var logStyleContext = new TerminalGUIStyleContext(fontProvider);
            var inputStyleContext = new TerminalGUIStyleContext(fontProvider);
            var promptStyleContext = new TerminalGUIStyleContext(fontProvider);
            var executeButtonsStyleContext = new TerminalGUIStyleContext(fontProvider);
            var openButtonsStyleContext = new TerminalGUIStyleContext(fontProvider);
            var logCopyButtonStyleContext = new TerminalGUIStyleContext(fontProvider);

            // Apply Colors immediately
            inputStyleContext.SetColor(theme.InputColor);
            promptStyleContext.SetColor(theme.PromptColor);
            executeButtonsStyleContext.SetColor(theme.ExecuteButtonColor);
            openButtonsStyleContext.SetColor(theme.ButtonColor);
            logCopyButtonStyleContext.SetColor(theme.CopyButtonColor);

            var cursorFlashSpeedProvider = new CursorFlashSpeedProvider(theme.CursorFlashSpeed);
            var buttonVisibleConfigurator = new TerminalButtonVisibleConfigurator
            {
                IsVisible = options.IsButtonVisible,
                IsReverse = options.IsButtonReverse,
            };

            // Renderers
            var windowRenderer = new TerminalWindowRenderer(pixelTexture2DRepository);
            windowRenderer.SetBackgroundColor(theme.BackgroundColor);
            var logCopyButtonRenderer = new TerminalLogCopyButtonRenderer(buttonVisibleConfigurator, logCopyButtonStyleContext);
            var logRenderer = new TerminalLogRenderer(logCopyButtonRenderer, logStyleContext, colorPaletteProvider);
            var inputRenderer = new TerminalInputRenderer(scrollConfigurator, inputStyleContext, colorPaletteProvider, cursorFlashSpeedProvider);
            var promptRenderer = new TerminalPromptRenderer(promptStyleContext) { Prompt = options.Prompt };
            var executeButtonRenderer = new TerminalExecuteButtonRenderer(executeButtonsStyleContext);
            var buttonRenderer = new TerminalButtonRenderer(pixelTexture2DRepository, openButtonsStyleContext);

            // Presenters
            var windowPresenter = new TerminalWindowPresenter(animatorDataConfigurator, new TerminalWindowAnimator());
            var logPresenter = new TerminalLogPresenter(domain.Service);
            var inputPresenter = new TerminalInputPresenter(inputRenderer, options.BootupCommand);
            var executeButtonPresenter = new TerminalExecuteButtonPresenter(executeButtonRenderer, buttonVisibleConfigurator);
            var buttonPresenter = new TerminalButtonPresenter(buttonRenderer, windowPresenter, buttonVisibleConfigurator);

            // View
            var viewContext = new TerminalViewContext
            {
                WindowRenderer = windowRenderer,
                LogRenderer = logRenderer,
                InputRenderer = inputRenderer,
                PromptRenderer = promptRenderer,
                ExecuteButtonRenderer = executeButtonRenderer,
                ButtonRenderer = buttonRenderer,
                LogCopyButtonRenderer = logCopyButtonRenderer,

                WindowRenderDataProvider = windowPresenter,
                LogRenderDataProvider = logPresenter,
                InputRenderDataProvider = inputPresenter,
                ExecuteButtonRenderDataProvider = executeButtonPresenter,
                ButtonRenderDataProvider = buttonPresenter,

                ScrollConfigurator = scrollConfigurator,
            };
            var view = new TerminalView(viewContext);

            return new RenderingContext
            {
                View = view,
                ScrollConfigurator = scrollConfigurator,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                ExecuteButtonPresenter = executeButtonPresenter,
                ButtonPresenter = buttonPresenter,
                
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
                    promptStyleContext,
                    viewContext,
                    
                    windowRenderer,
                    logCopyButtonRenderer,
                    logRenderer,
                    promptRenderer,
                    executeButtonRenderer,

                    windowPresenter,
                    logPresenter,
                    inputPresenter,
                    executeButtonPresenter,
                    buttonPresenter,
                    
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
            var eventListener = new TerminalEventListener(inputHandler);

            var coordinator = new TerminalCoordinator(
                domain.Service,
                rendering.View,
                rendering.ScrollConfigurator,
                rendering.WindowPresenter,
                rendering.InputPresenter,
                rendering.ExecuteButtonPresenter,
                rendering.ButtonPresenter,
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

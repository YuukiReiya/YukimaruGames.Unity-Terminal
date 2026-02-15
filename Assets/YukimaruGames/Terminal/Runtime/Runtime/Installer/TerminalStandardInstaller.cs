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
        [SerializeReference, SerializeInterface] 
        private ITerminalTheme _theme = new TerminalStandardTheme();

        [SerializeReference, SerializeInterface] 
        private ITerminalOptions _options = new TerminalStandardOptions();

        TerminalRuntimeScope ITerminalInstaller.Install()
        {
            var theme = _theme;
            var options = _options;
            
            var animatorDataConfigurator = new TerminalWindowAnimatorDataConfigurator()
            {
                State = theme.BootupWindowState,
                Anchor = theme.Anchor,
                Style = theme.WindowStyle,
                Duration = theme.Duration,
                Scale = theme.CompactScale,
            };
            var logger = new CommandLogger(options.BufferSize);

            var scrollConfigurator = new ScrollConfigurator();
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
                autocomplete);

            var specs = discover.Discover();
            var factory = new CommandFactory();
            foreach (var spec in specs)
            {
                var commandHandler = factory.Create(spec.Method);
                if (registry.Add(spec.Meta.Command, commandHandler))
                {
                    autocomplete.Register(spec.Meta.Command);
                }
            }

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
                IsReverse = options.IsButtonReverse
            };
                
            var windowRenderer = new TerminalWindowRenderer(pixelTexture2DRepository);
            windowRenderer.SetBackgroundColor(theme.BackgroundColor);
            
            var logCopyButtonRenderer = new TerminalLogCopyButtonRenderer(buttonVisibleConfigurator, logCopyButtonStyleContext);
            var logRenderer = new TerminalLogRenderer(logCopyButtonRenderer, logStyleContext, colorPaletteProvider);
            var inputRenderer = new TerminalInputRenderer(scrollConfigurator, inputStyleContext, colorPaletteProvider, cursorFlashSpeedProvider);
            var promptRenderer = new TerminalPromptRenderer(promptStyleContext) { Prompt = options.Prompt };
            var executeButtonRenderer = new TerminalExecuteButtonRenderer(executeButtonsStyleContext);
            var buttonRenderer = new TerminalButtonRenderer(pixelTexture2DRepository, openButtonsStyleContext);
            
            var windowPresenter = new TerminalWindowPresenter(animatorDataConfigurator, new TerminalWindowAnimator());
            var logPresenter = new TerminalLogPresenter(service);
            var inputPresenter = new TerminalInputPresenter(inputRenderer, options.BootupCommand);
            var executeButtonPresenter = new TerminalExecuteButtonPresenter(executeButtonRenderer, buttonVisibleConfigurator);
            var buttonPresenter = new TerminalButtonPresenter(buttonRenderer, windowPresenter, buttonVisibleConfigurator);
            
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
                
                ScrollConfigurator = scrollConfigurator
            };

            var view = new TerminalView(viewContext);
            
            // Resolve Keyboard Type
            var keyboardType = ResolveKeyboardType(options);
            var inputHandler = CreateInputHandler(options, keyboardType);
            var eventListener = new TerminalEventListener(inputHandler);
            
            var coordinator = new TerminalCoordinator(
                service,
                view,
                scrollConfigurator,
                windowPresenter,
                inputPresenter,
                executeButtonPresenter,
                buttonPresenter,
                eventListener);
            
            var instances = new object[]
            {
                scrollConfigurator,
                factory,

                // domain
                service,
                logger,
                invoker,
                parser,
                history,
                discover,
                registry,
                autocomplete,
                
                // provider.
                animatorDataConfigurator,
                colorPaletteProvider,
                fontProvider,
                pixelTexture2DRepository,
                
                // listener.
                eventListener,
                
                // context.
                logStyleContext,
                inputStyleContext,
                promptStyleContext,
                executeButtonsStyleContext,
                openButtonsStyleContext,
                logCopyButtonStyleContext,
                
                cursorFlashSpeedProvider,
                buttonVisibleConfigurator,
                
                // renderer.
                windowRenderer,
                logCopyButtonRenderer,
                logRenderer,
                inputRenderer,
                promptRenderer,
                executeButtonRenderer,
                buttonRenderer,
                view,
                
                // presenter.
                windowPresenter,
                logPresenter,
                inputPresenter,
                executeButtonPresenter,
                buttonPresenter,
                coordinator,
            };
            
            var updatables = instances.OfType<IUpdatable>().ToList();
            var disposables = instances.OfType<IDisposable>().ToList();

            var entryPoint = new TerminalEntryPoint(updatables, keyboardType, view);

            return new TerminalRuntimeScope(entryPoint, service, registry, autocomplete, disposables);
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
    }
}

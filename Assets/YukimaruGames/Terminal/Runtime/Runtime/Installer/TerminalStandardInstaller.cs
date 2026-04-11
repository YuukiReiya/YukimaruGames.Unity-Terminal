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
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Repositories;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.Infrastructure.Commands;
using YukimaruGames.Terminal.Infrastructure.UI;
using YukimaruGames.Terminal.Presentation.Accessors;
using YukimaruGames.Terminal.Presentation.Accessors.Launcher;
using YukimaruGames.Terminal.Presentation.Accessors.Scroll;
using YukimaruGames.Terminal.Presentation.Coordinators;
using YukimaruGames.Terminal.Presentation.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Interfaces.Repositories;
using YukimaruGames.Terminal.Presentation.Models;
using YukimaruGames.Terminal.Presentation.Presenters.Input;
using YukimaruGames.Terminal.Presentation.Presenters.Launcher;
using YukimaruGames.Terminal.Presentation.Presenters.Log;
using YukimaruGames.Terminal.Presentation.Presenters.Submit;
using YukimaruGames.Terminal.Presentation.Presenters.Window;
using YukimaruGames.Terminal.Presentation.Renderers.Clipboard;
using YukimaruGames.Terminal.Presentation.Renderers.Input;
using YukimaruGames.Terminal.Presentation.Renderers.Launcher;
using YukimaruGames.Terminal.Presentation.Renderers.Log;
using YukimaruGames.Terminal.Presentation.Renderers.Prompt;
using YukimaruGames.Terminal.Presentation.Renderers.Submit;
using YukimaruGames.Terminal.Presentation.Renderers.Window;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
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
            
            /// <inheritdoc cref="ITerminalWindow"/> 
            public ITerminalWindow View;
            /// <inheritdoc cref="IScrollMutator"/>
            public IScrollMutator ScrollMutator;
            /// <inheritdoc cref="IAnimationDataAccessor"/>
            public IAnimationDataAccessor AnimationDataAccessor;
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
            
            /// <inheritdoc cref="Coordinator"/> 
            public TerminalCoordinator Coordinator;
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

        #region runtime-instances

        [NonSerialized] private FontAccessor _fontAccessor;
        [NonSerialized] private ColorPaletteAccessor _colorPaletteAccessor;
        [NonSerialized] private AnimationDataAccessor _animationDataAccessor;
        [NonSerialized] private LauncherVisibleAccessor _launcherVisibleAccessor;
        [NonSerialized] private IWindowRenderer _windowRenderer;
        [NonSerialized] private IPromptRenderer _promptRenderer;
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
            _animationDataAccessor = null;
            _launcherVisibleAccessor = null;
            _windowRenderer = null;
            _promptRenderer = null;
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
                _colorPaletteAccessor[Constants.ThemeLabel.Message] = theme.MessageColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Entry] = theme.EntryColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Warning] = theme.WarningColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Error] = theme.ErrorColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Assert] = theme.AssertColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Exception] = theme.ExceptionColor;
                _colorPaletteAccessor[Constants.ThemeLabel.System] = theme.SystemColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Cursor] = theme.CaretColor;
                _colorPaletteAccessor[Constants.ThemeLabel.Selection] = theme.SelectionColor;
            }

            _pixelTextureRepository?.SetColor(Constants.ThemeLabel.Window, theme.BackgroundColor);
        }

        private void SyncAnimation(ITerminalAnimation animation)
        {
            if (_animationDataAccessor == null) return;
            
            _animationDataAccessor.Anchor = animation.Anchor;
            _animationDataAccessor.Style = animation.WindowStyle;
            _animationDataAccessor.Duration = animation.Duration;
            _animationDataAccessor.Scale = animation.CompactScale;
        }

        private void SyncOptions(ITerminalOptions options)
        {
            if (_launcherVisibleAccessor != null)
            {
                _launcherVisibleAccessor.IsVisible = options.IsButtonVisible;
                _launcherVisibleAccessor.IsReverse = options.IsButtonReverse;
            }

            if (_promptRenderer != null)
            {
                _promptRenderer.Prompt = options.Prompt;
            }
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
            _animationDataAccessor = new AnimationDataAccessor()
            {
                State = animation.BootupWindowState,
                Anchor = animation.Anchor,
                Style = animation.WindowStyle,
                Duration = animation.Duration,
                Scale = animation.CompactScale,
            };

            _colorPaletteAccessor = new ColorPaletteAccessor(new Dictionary<string, Color>
            {
                { Constants.ThemeLabel.Message, theme.MessageColor },
                { Constants.ThemeLabel.Entry, theme.EntryColor },
                { Constants.ThemeLabel.Warning, theme.WarningColor },
                { Constants.ThemeLabel.Error, theme.ErrorColor },
                { Constants.ThemeLabel.Assert, theme.AssertColor },
                { Constants.ThemeLabel.Exception, theme.ExceptionColor },
                { Constants.ThemeLabel.System, theme.SystemColor },
                { Constants.ThemeLabel.Cursor, theme.CaretColor },
                { Constants.ThemeLabel.Selection, theme.SelectionColor },
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
            
            var clipboardRenderer = new ClipboardRenderer(_launcherVisibleAccessor, _logCopyButtonGUIStyleAccessor);
            var logRenderer = new LogRenderer(clipboardRenderer, _logGUIStyleAccessor, _colorPaletteAccessor);
            var inputRenderer = new InputRenderer(scrollAccessor, _inputGUIStyleAccessor, _colorPaletteAccessor, _cursorFlashSpeedAccessor);
            _promptRenderer = new PromptRenderer(_promptGUIStyleAccessor) { Prompt = options.Prompt };
            var executeButtonRenderer = new SubmitRenderer(_executeButtonsGUIStyleAccessor);
            var launcherRenderer = new LauncherRenderer(_pixelTextureRepository, _launcherGUIStyleAccessor);

            // Presenters
            var windowPresenter = new WindowPresenter(_animationDataAccessor,  new WindowAnimator());
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var executeButtonPresenter = new SubmitPresenter(executeButtonRenderer, _launcherVisibleAccessor);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, _launcherVisibleAccessor, _animationDataAccessor);

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
            var view = new TerminalView(viewContext);

            return new RenderingContext
            {
                View = view,
                ScrollMutator = scrollAccessor,
                AnimationDataAccessor = _animationDataAccessor,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                LogPresenter = logPresenter,
                SubmitPresenter = executeButtonPresenter,
                LauncherPresenter = launcherPresenter,
                
                Components = new object[]
                {
                    _animationDataAccessor,
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
                    clipboardRenderer,
                    logRenderer,
                    inputRenderer,
                    _promptRenderer,
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

            var coordinator = new TerminalCoordinator(
                domain.Service,
                rendering.View,
                rendering.ScrollMutator,
                rendering.AnimationDataAccessor,
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

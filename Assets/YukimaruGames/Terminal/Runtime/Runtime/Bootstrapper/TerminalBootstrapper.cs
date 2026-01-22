#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_INPUT_SYSTEM 
//#undef ENABLE_LEGACY_INPUT_MANAGER

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Runtime.Input.LegacyInput;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Service;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Infrastructure;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.Domain.Settings;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;
using YukimaruGames.Terminal.UI.View.Input;
using YukimaruGames.Terminal.UI.View.Model;
using ColorPalette=YukimaruGames.Terminal.SharedKernel.Constants.Constants.ColorPalette;

namespace YukimaruGames.Terminal.Runtime
{
    public sealed partial class TerminalBootstrapper : MonoBehaviour,IDisposable
    {
        // view
        [Header("Settings")]
        [SerializeInterface]
        [SerializeReference]
        private ITerminalTheme _theme = new TerminalTheme();

        [SerializeInterface]
        [SerializeReference]
        private ITerminalOptions _options = new TerminalOptions();
        
        private readonly List<IUpdatable> _updatables = new();
        private readonly List<IDisposable> _disposables = new();

        private InputKeyboardType KeyboardType => _options?.InputKeyboardType ?? InputKeyboardType.None;
        
        // orchestrator
        private TerminalCoordinator _coordinator;
        private ITerminalView _view;
        
        // listener.
        private TerminalEventListener _eventListener;
        
        // domain.
        private ICommandLogger _logger;
        private ICommandRegistry _registry;
        private ICommandAutocomplete _autocomplete;

        // configurator.
        private TerminalWindowAnimatorDataConfigurator _animatorDataConfigurator;
        
        // repository
        private IPixelTexture2DRepository _pixelTexture2DRepository;
        
        // color-palette provider.
        private ColorPaletteProvider _colorPaletteProvider;
        
        // font provider.
        private TerminalFontProvider _fontProvider;
        
        // gui-style provider.
        private TerminalGUIStyleContext _logStyleContext;
        private TerminalGUIStyleContext _inputStyleContext;
        private TerminalGUIStyleContext _promptStyleContext;
        private TerminalGUIStyleContext _executeButtonsStyleContext;
        private TerminalGUIStyleContext _openButtonsStyleContext;
        private TerminalGUIStyleContext _logCopyButtonStyleContext;
        
        // cursor-flash-speed provider.
        private CursorFlashSpeedProvider _cursorFlashSpeedProvider;
        
        // button-visible
        private TerminalButtonVisibleConfigurator _buttonVisibleConfigurator; 
        
        // renderer.
        private TerminalWindowRenderer _windowRenderer;
        private TerminalLogRenderer _logRenderer;
        private TerminalInputRenderer _inputRenderer;
        private TerminalPromptRenderer _promptRenderer;
        private TerminalExecuteButtonRenderer _executeButtonRenderer;
        private ITerminalButtonRenderer _buttonRenderer;
        private TerminalLogCopyButtonRenderer _logCopyButtonRenderer;
        
        // presenter.
        private TerminalWindowPresenter _windowPresenter;
        private TerminalLogPresenter _logPresenter;
        private TerminalInputPresenter _inputPresenter;
        private TerminalExecuteButtonPresenter _executeButtonPresenter;
        private TerminalButtonPresenter _buttonPresenter;
        
        // application-service.
        private ITerminalService _service;
        
        private void Awake()
        {
            if (_theme == null) _theme = new TerminalTheme();
            if (_options == null) _options = new TerminalOptions();

            _animatorDataConfigurator = new TerminalWindowAnimatorDataConfigurator()
            {
                State = _options.BootupWindowState,
                Anchor = _options.Anchor,
                Style = _options.WindowStyle,
                Duration = _theme.Duration,
                Scale = _theme.CompactScale,
            };
            _logger = new CommandLogger(_options.BufferSize);

            var scrollConfigurator = new ScrollConfigurator();
            _registry = new CommandRegistry(_logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var discover = new CommandDiscoverer(_logger);
            _autocomplete = new CommandAutocomplete(); 
            _service = new TerminalService(
                _logger,
                _registry,
                invoker,
                parser,
                history,
                _autocomplete);

            var specs = discover.Discover();
            var factory = new CommandFactory();
            foreach (var spec in specs)
            {
                var commandHandler = factory.Create(spec.Method);
                if (_registry.Add(spec.Meta.Command, commandHandler))
                {
                    _autocomplete.Register(spec.Meta.Command);
                }
            }

            _colorPaletteProvider = new ColorPaletteProvider(new Dictionary<string, Color>
            {
                { ColorPalette.Message, _theme.Message.ToUnityColor() },
                { ColorPalette.Entry, _theme.Entry.ToUnityColor() },
                { ColorPalette.Warning, _theme.Warning.ToUnityColor() },
                { ColorPalette.Error, _theme.Error.ToUnityColor() },
                { ColorPalette.Assert, _theme.Assert.ToUnityColor() },
                { ColorPalette.Exception, _theme.Exception.ToUnityColor() },
                { ColorPalette.System, _theme.System.ToUnityColor() },
                
                { ColorPalette.Cursor, _theme.Caret.ToUnityColor() },
                { ColorPalette.Selection, _theme.Selection.ToUnityColor() },
            });
            
            _fontProvider = new TerminalFontProvider(((_theme as TerminalTheme)?.Font));

            _pixelTexture2DRepository = new PixelTexture2DRepository();

            _logStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _inputStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _promptStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _executeButtonsStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _openButtonsStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _logCopyButtonStyleContext = new TerminalGUIStyleContext(_fontProvider);
            
            _cursorFlashSpeedProvider = new CursorFlashSpeedProvider(_theme.CursorFlashSpeed);
            _buttonVisibleConfigurator = new TerminalButtonVisibleConfigurator();
                
            _windowRenderer = new TerminalWindowRenderer(_pixelTexture2DRepository);
            _logCopyButtonRenderer = new TerminalLogCopyButtonRenderer(_buttonVisibleConfigurator, _logCopyButtonStyleContext);
            _logRenderer = new TerminalLogRenderer(_logCopyButtonRenderer,_logStyleContext, _colorPaletteProvider);
            _inputRenderer = new TerminalInputRenderer(scrollConfigurator, _inputStyleContext, _colorPaletteProvider, _cursorFlashSpeedProvider);
            _promptRenderer = new TerminalPromptRenderer(_promptStyleContext);
            _executeButtonRenderer = new TerminalExecuteButtonRenderer(_executeButtonsStyleContext);
            _buttonRenderer = new TerminalButtonRenderer(_pixelTexture2DRepository, _openButtonsStyleContext);
            
            _windowPresenter = new TerminalWindowPresenter(_animatorDataConfigurator, new TerminalWindowAnimator());
            _logPresenter = new TerminalLogPresenter(_service);
            _inputPresenter = new TerminalInputPresenter(_inputRenderer, _options.BootupCommand);
            _executeButtonPresenter = new TerminalExecuteButtonPresenter(_executeButtonRenderer, _buttonVisibleConfigurator);
            _buttonPresenter = new TerminalButtonPresenter(_buttonRenderer, _windowPresenter, _buttonVisibleConfigurator);
            
            var viewContext = new TerminalViewContext
            {
                WindowRenderer = _windowRenderer,
                LogRenderer = _logRenderer,
                InputRenderer = _inputRenderer,
                PromptRenderer = _promptRenderer,
                ExecuteButtonRenderer = _executeButtonRenderer,
                ButtonRenderer = _buttonRenderer,
                LogCopyButtonRenderer = _logCopyButtonRenderer,
                
                WindowRenderDataProvider = _windowPresenter,
                LogRenderDataProvider = _logPresenter,
                InputRenderDataProvider = _inputPresenter,
                ExecuteButtonRenderDataProvider = _executeButtonPresenter,
                ButtonRenderDataProvider = _buttonPresenter,
                
                ScrollConfigurator = scrollConfigurator
            };
            
            _promptStyleContext?.SetColor(_theme.Prompt.ToUnityColor());

            _view = new TerminalView(viewContext);

            _eventListener = new TerminalEventListener(CreateInputHandler());
            _coordinator = new TerminalCoordinator(
                _service,
                _view,
                scrollConfigurator,
                _windowPresenter,
                _inputPresenter,
                _executeButtonPresenter,
                _buttonPresenter,
                _eventListener);
            
            var instances = new object[]
            {
                scrollConfigurator,
                factory,

                // domain
                _service,
                _logger,
                invoker,
                parser,
                history,
                discover,
                _registry,
                _autocomplete,
                
                // provider.
                _animatorDataConfigurator,
                _colorPaletteProvider,
                _fontProvider,
                _pixelTexture2DRepository,
                
                // listener.
                _eventListener,
                
                // context.
                _logStyleContext,
                _inputStyleContext,
                _promptStyleContext,
                _executeButtonsStyleContext,
                _openButtonsStyleContext,
                _logCopyButtonStyleContext,
                
                _cursorFlashSpeedProvider,
                _buttonVisibleConfigurator,
                
                // renderer.
                _windowRenderer,
                _logCopyButtonRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _executeButtonRenderer,
                _buttonRenderer,
                _view,
                
                // presenter.
                _windowPresenter,
                _logPresenter,
                _inputPresenter,
                _executeButtonPresenter,
                _buttonPresenter,
                _coordinator,
            };
            _updatables.AddRange(instances.OfType<IUpdatable>());
            _disposables.AddRange(instances.OfType<IDisposable>());
            Configure();
        }

        private void Update()
        {
            if (KeyboardType is InputKeyboardType.InputSystem)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                // NOTE:foreachでアクセスするとコレクションから実体にアクセスする際に多少のオーバヘッドが生じてしまうためforを採用する.
                for (var i = 0; i < _updatables.Count; ++i) _updatables[i]?.Update(Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            if (KeyboardType is InputKeyboardType.Legacy)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _updatables.Count; ++i) _updatables[i]?.Update(Time.deltaTime);
            }
            _view.Render();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        [Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            if (!UnityEngine.Application.isPlaying) return;
            Configure();
        }

        private void Configure()
        {
            if (_theme == null) _theme = new TerminalTheme();
            if (_options == null) _options = new TerminalOptions();

            if (_animatorDataConfigurator != null)
            {
                _animatorDataConfigurator.Anchor = _options.Anchor;
                _animatorDataConfigurator.Style = _options.WindowStyle;
                _animatorDataConfigurator.Scale = _theme.CompactScale;
                _animatorDataConfigurator.Duration = _theme.Duration;
            }

            if (_fontProvider != null)
            {
                _fontProvider.Font = (_theme as TerminalTheme)?.Font;
                _fontProvider.Size = _theme.FontSize;
            }
            
            _windowRenderer?.SetBackgroundColor(_theme.Background.ToUnityColor());

            if (_colorPaletteProvider != null)
            {
                _colorPaletteProvider.SetColor(ColorPalette.Error, _theme.Error.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.Assert, _theme.Assert.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.Warning, _theme.Warning.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.Message, _theme.Message.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.Exception, _theme.Exception.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.Entry, _theme.Entry.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.System, _theme.System.ToUnityColor());
                
                _colorPaletteProvider.SetColor(ColorPalette.Cursor, _theme.Caret.ToUnityColor());
                _colorPaletteProvider.SetColor(ColorPalette.Selection, _theme.Selection.ToUnityColor());
            }

            if (_promptRenderer != null)
            {
                _promptRenderer.Prompt = _options.Prompt;
                _promptStyleContext.SetColor(_theme.Prompt.ToUnityColor());
            }

            _inputStyleContext?.SetColor(_theme.Input.ToUnityColor());

            if (_theme is IButtonThemeProvider provider)
            {
                var buttonTheme = provider.ButtonTheme;
                _executeButtonsStyleContext?.SetColor(buttonTheme.Execute.ToUnityColor());
                _openButtonsStyleContext?.SetColor(buttonTheme.Base.ToUnityColor());
                _logCopyButtonStyleContext?.SetColor(buttonTheme.Copy.ToUnityColor());
            }

            _eventListener?.SetInputHandler(CreateInputHandler());
            _cursorFlashSpeedProvider?.SetFlashSpeed(_theme.CursorFlashSpeed);
            
            if (_buttonVisibleConfigurator != null)
            {
                _buttonVisibleConfigurator.IsVisible = _options.ButtonVisible;
                _buttonVisibleConfigurator.IsReverse = _options.ButtonReverse;
            }
        }

        private IKeyboardInputHandler CreateInputHandler()
        {
            var options = _options as TerminalOptions;
            var factory =
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(options?.InputSystemKey, options?.LegacyInputKey);
#elif ENABLE_INPUT_SYSTEM
                new TerminalKeyboardFactory(options?.InputSystemKey);
#elif ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(options?.LegacyInputKey);
#else
                new TerminalKeyboardFactory();
#endif
            return factory.Create(KeyboardType);
        }
        
        public void Dispose()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; i++)
            {
                _disposables[i]?.Dispose();
            }
            _disposables.Clear();
        }
    }
    public static class TerminalColorExtensions
    {
        public static Color ToUnityColor(this TerminalColor color) => new(color.R, color.G, color.B, color.A);
    }
}

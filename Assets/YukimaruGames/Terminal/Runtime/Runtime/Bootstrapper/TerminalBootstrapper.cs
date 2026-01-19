#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif
//#undef ENABLE_INPUT_SYSTEM 
//#undef ENABLE_LEGACY_INPUT_MANAGER

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
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;
using YukimaruGames.Terminal.UI.View.Input;
using YukimaruGames.Terminal.UI.View.Model;
using ColorPalette=YukimaruGames.Terminal.SharedKernel.Constants.Constants.ColorPalette;

namespace YukimaruGames.Terminal.Runtime
{
    public sealed partial class TerminalBootstrapper : MonoBehaviour, IDisposable
    {
        [SerializeReference, SerializeInterface]
        private ITerminalInstaller _installer;

        private void Start()
        {
            _installer.Install();
        }

        private void Update()
        {
            //if (_installer?.KeyboardInput is InputSystemKeyboardHandler)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                // NOTE:foreachでアクセスするとコレクションから実体にアクセスする際に多少のオーバヘッドが生じてしまうためforを採用する.
                for (var i = 0; i < _installer?.Updatables.Count; i++) _installer?.Updatables[i]?.Update(Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            //if (_installer?.KeyboardInput is LegacyInputKeyboardHandler)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                // NOTE:foreachでアクセスするとコレクションから実体にアクセスする際に多少のオーバヘッドが生じてしまうためforを採用する.
                //for (var i = 0; i < _installer?.Updatables.Count; i++) _installer?.Updatables[i]?.Update(Time.deltaTime);
            }
            
            _installer?.View?.Render();
        }

        private void OnDestroy()
        {
            if (this is IDisposable self)
            {
                self.Dispose();
            }
        }

        void IDisposable.Dispose()
        {
            _installer.Uninstall();
        }

        [Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            if (!UnityEngine.Application.isPlaying) return;
            _installer.Apply();
        }
#if false
        // view
        [SerializeField] private Font _font;
        [SerializeField] private int _fontSize = 55;
        [SerializeField] private Color _backgroundColor = Color.black;
        [SerializeField] private Color _messageColor = Color.white;
        [SerializeField] private Color _entryColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private Color _assertColor = Color.red;
        [SerializeField] private Color _exceptionColor = Color.red;
        [SerializeField] private Color _systemColor = Color.white;
        [SerializeField] private Color _inputColor = new(0f, 1f, 0.3f);
        [SerializeField] private Color _caretColor = new(0f, 1f, 0.8f);
        [SerializeField] private Color _selectionColor = new(1f, 0.5f, 0f);
        [SerializeField] private Color _promptColor = new(0f, 0.8f, 0.15f);
        [SerializeField] private Color _executeButtonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _buttonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _copyButtonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private float _cursorFlashSpeed = 1.886792f;

        // input
        [SerializeField] private InputKeyboardType _inputKeyboardType = InputKeyboardType.InputSystem;

#if ENABLE_LEGACY_INPUT_MANAGER
        // legacy
        [SerializeField] private LegacyInputKey _legacyInputKey;
#endif

#if ENABLE_INPUT_SYSTEM
        // input system
        [SerializeField] private InputSystemKey _inputSystemKey;
#endif

        [SerializeField] private TerminalState _bootupWindowState = TerminalState.Close;
        [SerializeField] private TerminalAnchor _anchor = TerminalAnchor.Top;
        [SerializeField] private TerminalWindowStyle _windowStyle = TerminalWindowStyle.Compact;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _compactScale = 0.35f;
        [SerializeField] private int _bufferSize = 256;
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;

        private readonly List<IUpdatable> _updatables = new();
        private readonly List<IDisposable> _disposables = new();

        private InputKeyboardType KeyboardType =>
#if ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            _inputKeyboardType;
#elif ENABLE_INPUT_SYSTEM
            InputKeyboardType.InputSystem;
#elif ENABLE_LEGACY_INPUT_MANAGER
            InputKeyboardType.Legacy;
#else
            InputKeyboardType.None;
#endif

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
        private TerminalWindowAnimatorDataMutator _animatorDataMutator;

        // repository
        private IPixelTexture2DRepository _pixelTexture2DRepository;

        // color-palette provider.
        private ColorPaletteContext _colorPaletteProvider;

        // font provider.
        private TerminalFontContext _fontProvider;

        // gui-style provider.
        private TerminalGUIStyleContext _logStyleContext;
        private TerminalGUIStyleContext _inputStyleContext;
        private TerminalGUIStyleContext _promptStyleContext;
        private TerminalGUIStyleContext _executeButtonsStyleContext;
        private TerminalGUIStyleContext _openButtonsStyleContext;
        private TerminalGUIStyleContext _logCopyButtonStyleContext;

        // cursor-flash-speed provider.
        private CursorFlashContext _cursorFlashContext;

        // button-visible
        private TerminalButtonVisibleMutator _buttonVisibleMutator;

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
            _animatorDataMutator = new TerminalWindowAnimatorDataMutator()
            {
                State = _bootupWindowState,
                Anchor = _anchor,
                Style = _windowStyle,
                Duration = _duration,
                Scale = _compactScale,
            };
            _logger = new CommandLogger(_bufferSize);

            var scrollConfigurator = new ScrollMutator();
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

            _colorPaletteProvider = new ColorPaletteContext(new Dictionary<string, Color>
            {
                { ColorPalette.Message, _messageColor },
                { ColorPalette.Entry, _entryColor },
                { ColorPalette.Warning, _warningColor },
                { ColorPalette.Error, _errorColor },
                { ColorPalette.Assert, _assertColor },
                { ColorPalette.Exception, _exceptionColor },
                { ColorPalette.System, _systemColor },

                { ColorPalette.Cursor, _caretColor },
                { ColorPalette.Selection, _selectionColor },
            });

            _fontProvider = new TerminalFontContext(_font);

            _pixelTexture2DRepository = new PixelTexture2DRepository();

            _logStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _inputStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _promptStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _executeButtonsStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _openButtonsStyleContext = new TerminalGUIStyleContext(_fontProvider);
            _logCopyButtonStyleContext = new TerminalGUIStyleContext(_fontProvider);

            _cursorFlashContext = new CursorFlashContext(_cursorFlashSpeed);
            _buttonVisibleMutator = new TerminalButtonVisibleMutator();

            _windowRenderer = new TerminalWindowRenderer(_pixelTexture2DRepository);
            _logCopyButtonRenderer = new TerminalLogCopyButtonRenderer(_buttonVisibleMutator, _logCopyButtonStyleContext);
            _logRenderer = new TerminalLogRenderer(_logCopyButtonRenderer, _logStyleContext, _colorPaletteProvider);
            _inputRenderer = new TerminalInputRenderer(scrollConfigurator, _inputStyleContext, _colorPaletteProvider, _cursorFlashContext);
            _promptRenderer = new TerminalPromptRenderer(_promptStyleContext);
            _executeButtonRenderer = new TerminalExecuteButtonRenderer(_executeButtonsStyleContext);
            _buttonRenderer = new TerminalButtonRenderer(_pixelTexture2DRepository, _openButtonsStyleContext);

            _windowPresenter = new TerminalWindowPresenter(_animatorDataMutator, new TerminalWindowAnimator());
            _logPresenter = new TerminalLogPresenter(_service);
            _inputPresenter = new TerminalInputPresenter(_inputRenderer, _bootupCommand);
            _executeButtonPresenter = new TerminalExecuteButtonPresenter(_executeButtonRenderer, _buttonVisibleMutator);
            _buttonPresenter = new TerminalButtonPresenter(_buttonRenderer, _windowPresenter, _buttonVisibleMutator);

            _view = new TerminalView(
                _windowRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _executeButtonRenderer,
                _buttonRenderer,
                _logCopyButtonRenderer,
                _windowPresenter,
                _logPresenter,
                _inputPresenter,
                _executeButtonPresenter,
                _buttonPresenter,
                scrollConfigurator);

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
                _animatorDataMutator,
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

                _buttonVisibleMutator,

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
            IDisposable disposer = this;
            disposer.Dispose();
        }

        [Conditional("UNITY_EDITOR")]
        private void OnValidate()
        {
            if (!UnityEngine.Application.isPlaying) return;
            Configure();
        }

        private void Configure()
        {
            if (_animatorDataMutator != null)
            {
                _animatorDataMutator.Anchor = _anchor;
                _animatorDataMutator.Style = _windowStyle;
                _animatorDataMutator.Scale = _compactScale;
                _animatorDataMutator.Duration = _duration;
            }

            if (_fontProvider != null)
            {
                _fontProvider.Font = _font;
                _fontProvider.Size = _fontSize;
            }

            //_windowRenderer?.SetBackgroundColor(_backgroundColor);

            if (_colorPaletteProvider is IColorPaletteMutator colorPaletteMutator)
            {
                colorPaletteMutator.SetColor(ColorPalette.Error, _errorColor);
                colorPaletteMutator.SetColor(ColorPalette.Assert, _assertColor);
                colorPaletteMutator.SetColor(ColorPalette.Warning, _warningColor);
                colorPaletteMutator.SetColor(ColorPalette.Message, _messageColor);
                colorPaletteMutator.SetColor(ColorPalette.Exception, _exceptionColor);
                colorPaletteMutator.SetColor(ColorPalette.Entry, _entryColor);
                colorPaletteMutator.SetColor(ColorPalette.System, _systemColor);

                colorPaletteMutator.SetColor(ColorPalette.Cursor, _caretColor);
                colorPaletteMutator.SetColor(ColorPalette.Selection, _selectionColor);
            }

            if (_promptRenderer != null)
            {
                _promptRenderer.Prompt = _prompt;
                //_promptStyleContext.SetColor(_promptColor);
            }

            // _inputStyleContext?.SetColor(_inputColor);
            // _executeButtonsStyleContext?.SetColor(_executeButtonColor);
            // _openButtonsStyleContext?.SetColor(_buttonColor);
            // _logCopyButtonStyleContext?.SetColor(_copyButtonColor);
            _eventListener?.SetInputHandler(CreateInputHandler());
            //_cursorFlashContext?.SetFlashSpeed(_cursorFlashSpeed);

            if (_buttonVisibleMutator != null)
            {
                _buttonVisibleMutator.IsVisible = _buttonVisible;
                _buttonVisibleMutator.IsReverse = _buttonReverse;
            }
        }

        private IKeyboardInputHandler CreateInputHandler()
        {
            var factory =
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(_inputSystemKey, _legacyInputKey);
#elif ENABLE_INPUT_SYSTEM
                new TerminalKeyboardFactory(_inputSystemKey);
#elif ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(_legacyInputKey);
#else
                new TerminalKeyboardFactory();
#endif
            return factory.Create(KeyboardType);
        }

        void IDisposable.Dispose()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; i++)
            {
                _disposables[i]?.Dispose();
            }

            _disposables.Clear();
        }
    }
#endif
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Service;
using YukimaruGames.Terminal.Infrastructure;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.SharedKernel.Constants;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;
using YukimaruGames.Terminal.UI.View.Input;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable]
    public sealed class TerminalInstaller : ITerminalInstaller, IDisposable
    {
        [Header("Font")]
        [SerializeField] private Font _font;
        [SerializeField] private int _fontSize = 55;
        
        [Header("Color")]
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
        [SerializeField] private Color _windowControlButtonColor = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _copyButtonColor = new(0f, 0.7f, 0.8f);

        [Header("CursorFlash")]
        [SerializeField] private float _cursorFlashSpeed = 1.886792f;

        [Header("Animator")]
        [SerializeField] private TerminalState _bootupWindowState = TerminalState.Close;
        [SerializeField] private TerminalAnchor _anchor = TerminalAnchor.Top;
        [SerializeField] private TerminalWindowStyle _windowStyle = TerminalWindowStyle.Compact;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _compactScale = 0.35f;

        [Header("Logs")]
        [SerializeField] private int _bufferSize = 256;

        [Header("Command")]
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;

        [Header("Input")]
        [SerializeReference, SerializeInterface]
        private IKeyboardInputHandler _inputHandler;

        [Header("Button")]
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;
        
        private readonly List<IUpdatable> _updatables = new();
        private bool _installed;

        private const string kWindowBackgroundKey = "WindowGUIStyle";
        
        #region repository.

        private IPixelTexture2DRepository _pixelTexture2DRepository;

        #endregion

        #region domain.

        private ICommandLogger _logger;
        private ICommandRegistry _registry;
        private ICommandAutocomplete _autocomplete;

        #endregion

        #region gui-style.

        private Lazy<GUIStyle> _backgroundStyleLazy;

        #endregion

        #region context.

        private TerminalGUIStyleContext _logStyleContext;
        private TerminalGUIStyleContext _inputStyleContext;
        private TerminalGUIStyleContext _promptStyleContext;
        private TerminalGUIStyleContext _logCopyButtonStyleContext;
        private TerminalGUIStyleContext _executeButtonsStyleContext;
        private TerminalGUIStyleContext _windowControlButtonStyleContext;

        private CursorFlashContext _cursorFlashContext;

        #endregion

        #region mutator.

        private ITerminalWindowAnimatorDataMutator _animatorDataMutator;

        #endregion

        #region orchestrator.

        private TerminalCoordinator _coordinator;
        private ITerminalView _view;

        #endregion

        #region listener.

        private TerminalEventListener _eventListener;

        #endregion

        #region view.

        private ITerminalWindowRenderer _windowRenderer;
        private ITerminalLogRenderer _logRenderer;
        private ITerminalInputRenderer _inputRenderer;
        private ITerminalPromptRenderer _promptRenderer;
        private ITerminalExecuteButtonRenderer _executeButtonRenderer;
        private ITerminalLogCopyButtonRenderer _logCopyButtonRenderer;
        private ITerminalButtonRenderer _windowControlButtonRenderer;

        #region presenter.

        private ITerminalWindowPresenter _windowPresenter;
        private ITerminalLogPresenter _logPresenter;
        private ITerminalInputPresenter _inputPresenter;
        private ITerminalExecuteButtonPresenter _executeButtonPresenter;
        private TerminalButtonPresenter _windowControlButtonPresenter;

        #endregion

        #region application-service

        private ITerminalService _service;

        #endregion

        #endregion

        private ColorPaletteContext _colorPaletteContext;
        private TerminalFontContext _fontContext;

        private TerminalButtonVisibleContext _buttonVisibleContext;

        private readonly List<IDisposable> _disposables = new();

        #region Accessor

        IReadOnlyList<IUpdatable> ITerminalInstaller.Updatables => _updatables;
        IFontMutator ITerminalInstaller.FontMutator => _fontContext;
        IPixelTexture2DRepository ITerminalInstaller.PixelTexture2DRepository => _pixelTexture2DRepository;
        IColorPaletteMutator ITerminalInstaller.ColorPaletteMutator => _colorPaletteContext;
        IGUIStyleMutator ITerminalInstaller.LogStyleMutator => _logStyleContext;
        IGUIStyleMutator ITerminalInstaller.InputStyleMutator => _inputStyleContext;
        IGUIStyleMutator ITerminalInstaller.PromptStyleMutator => _promptStyleContext;
        IGUIStyleMutator ITerminalInstaller.ExecuteButtonStyleMutator => _executeButtonsStyleContext;
        IGUIStyleMutator ITerminalInstaller.WindowControlButtonStyleMutator => _windowControlButtonStyleContext;
        IGUIStyleMutator ITerminalInstaller.LogCopyButtonStyleMutator => _logCopyButtonStyleContext;
        ITerminalWindowAnimatorDataMutator ITerminalInstaller.WindowAnimatorDataMutator => _animatorDataMutator;
        ICursorFlashMutator ITerminalInstaller.CursorFlashMutator => _cursorFlashContext;
        ITerminalButtonVisibleMutator ITerminalInstaller.ButtonVisibleMutator => _buttonVisibleContext;
        ITerminalView ITerminalInstaller.View => _view;
        ITerminalEventListener ITerminalInstaller.EventListener => _eventListener;
        ITerminalService ITerminalInstaller.Service => _service;
        IKeyboardInputHandler ITerminalInstaller.KeyboardInput => _inputHandler;

        bool ITerminalInstaller.Installed => _installed;

        #endregion

        /// <inheritdoc/>
        void ITerminalInstaller.Install()
        {
            ITerminalInstaller installer = this;
            installer.Uninstall();

            _fontContext = new TerminalFontContext();
            _pixelTexture2DRepository = new PixelTexture2DRepository();
            _buttonVisibleContext = new TerminalButtonVisibleContext();
            var scrollMutator = new ScrollMutator();
            _cursorFlashContext = new CursorFlashContext(_cursorFlashSpeed);

            _colorPaletteContext = new ColorPaletteContext(new Dictionary<string, Color>()
            {
                { Constants.ColorPalette.Message, _messageColor },
                { Constants.ColorPalette.Entry, _entryColor },
                { Constants.ColorPalette.Warning, _warningColor },
                { Constants.ColorPalette.Error, _errorColor },
                { Constants.ColorPalette.Assert, _assertColor },
                { Constants.ColorPalette.Exception, _exceptionColor },
                { Constants.ColorPalette.System, _systemColor },

                { Constants.ColorPalette.Cursor, _caretColor },
                { Constants.ColorPalette.Selection, _selectionColor },
            });

            // domain
            _logger = new CommandLogger(_bufferSize);
            _registry = new CommandRegistry(_logger);
            _autocomplete = new CommandAutocomplete();
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var discover = new CommandDiscoverer(_logger);
            var factory = new CommandFactory();
            var specs = discover.Discover();
            foreach (var spec in specs)
            {
                var commandHandler = factory.Create(spec.Method);
                if (_registry.Add(spec.Meta.Command, commandHandler))
                {
                    _autocomplete.Register(spec.Meta.Command);
                }
            }

            // service
            _service = new TerminalService(_logger, _registry, invoker, parser, history, _autocomplete);

            // GUI style
            _pixelTexture2DRepository.SetColor(kWindowBackgroundKey, _backgroundColor);
            _backgroundStyleLazy = new Lazy<GUIStyle>(new GUIStyle
            {
                normal = new GUIStyleState
                {
                    background = _pixelTexture2DRepository.GetTexture2D(kWindowBackgroundKey)
                },
            });

            // GUI style context
            _logStyleContext = new TerminalGUIStyleContext(_fontContext);
            _logCopyButtonStyleContext = new TerminalGUIStyleContext(_fontContext);
            _inputStyleContext = new TerminalGUIStyleContext(_fontContext);
            _promptStyleContext = new TerminalGUIStyleContext(_fontContext);
            _executeButtonsStyleContext = new TerminalGUIStyleContext(_fontContext);
            _windowControlButtonStyleContext = new TerminalGUIStyleContext(_fontContext);

            // mutator.
            _animatorDataMutator = new TerminalWindowAnimatorDataMutator()
            {
                State = _bootupWindowState,
                Anchor = _anchor,
                Style = _windowStyle,
                Duration = _duration,
                Scale = _compactScale,
            };

            // renderer.

            _logCopyButtonRenderer = new TerminalLogCopyButtonRenderer(_buttonVisibleContext, _logStyleContext);
            _executeButtonRenderer = new TerminalExecuteButtonRenderer(_executeButtonsStyleContext);
            _windowControlButtonRenderer = new TerminalButtonRenderer(_pixelTexture2DRepository, _windowControlButtonStyleContext);
            _windowRenderer = new TerminalWindowRenderer(_backgroundStyleLazy.Value);
            _logRenderer = new TerminalLogRenderer(_logCopyButtonRenderer, _logCopyButtonStyleContext, _colorPaletteContext);
            _inputRenderer = new TerminalInputRenderer(scrollMutator, _inputStyleContext, _colorPaletteContext, _cursorFlashContext);
            _promptRenderer = new TerminalPromptRenderer(_promptStyleContext);

            // presenter.
            _windowPresenter = new TerminalWindowPresenter(_animatorDataMutator, new TerminalWindowAnimator());
            _logPresenter = new TerminalLogPresenter(_service);
            _inputPresenter = new TerminalInputPresenter(_inputRenderer, _bootupCommand);
            _executeButtonPresenter = new TerminalExecuteButtonPresenter(_executeButtonRenderer, _buttonVisibleContext);
            _windowControlButtonPresenter = new TerminalButtonPresenter(_windowControlButtonRenderer, _windowPresenter, _buttonVisibleContext);

            //listener
            _eventListener = new TerminalEventListener(_inputHandler);

            // orchestrator
            {
                // View
                _view = new TerminalView(
                    _windowRenderer,
                    _logRenderer,
                    _inputRenderer,
                    _promptRenderer,
                    _executeButtonRenderer,
                    _windowControlButtonRenderer,
                    _logCopyButtonRenderer,
                    _windowPresenter,
                    _logPresenter,
                    _inputPresenter,
                    _executeButtonPresenter,
                    _windowControlButtonPresenter,
                    scrollMutator
                );

                // Coordinator.
                _coordinator = new TerminalCoordinator(
                    _service,
                    _view,
                    scrollMutator,
                    _windowPresenter,
                    _inputPresenter,
                    _executeButtonPresenter,
                    _windowControlButtonPresenter,
                    _eventListener
                );
            }

            var instances = new object[]
            {
                // repository
                _pixelTexture2DRepository,

                // context
                _fontContext,
                _buttonVisibleContext,
                _cursorFlashContext,
                _colorPaletteContext,

                // mutator.
                scrollMutator,
                _animatorDataMutator,

                // domain.
                _logger,
                _registry,
                _autocomplete,
                invoker,
                parser,
                history,
                _service,

                // gui-style context.
                _logStyleContext,
                _logCopyButtonStyleContext,
                _inputStyleContext,
                _promptStyleContext,
                _executeButtonsStyleContext,

                // renderer.
                _windowRenderer,
                _logRenderer,
                _inputRenderer,
                _promptRenderer,
                _logCopyButtonRenderer,
                _executeButtonRenderer,
                _windowControlButtonRenderer,

                // presenter.
                _windowPresenter,
                _logPresenter,
                _inputPresenter,
                _executeButtonPresenter,

                // listener.
                _eventListener,

                // orchestrator.
                _view,
                _coordinator,
            };

            _updatables.AddRange(instances.OfType<IUpdatable>());
            _disposables.AddRange(instances.OfType<IDisposable>());
            _installed = true;
            
            Apply();
        }

        /// <inheritdoc/>
        void ITerminalInstaller.Uninstall()
        {
            _installed = false;
            IDisposable disposer = this;
            disposer.Dispose();
        }

        /// <inheritdoc/>
        public void Apply()
        {
            if (!_installed)
            {
                return;
            }

            if (_animatorDataMutator != null)
            {
                _animatorDataMutator.Anchor = _anchor;
                _animatorDataMutator.Style = _windowStyle;
                _animatorDataMutator.Scale = _compactScale;
                _animatorDataMutator.Duration = _duration;
            }

            if (_fontContext.Font != _font)
            {
                _fontContext.Font = _font;
            }

            if (_fontContext.Size != _fontSize)
            {
                _fontContext.Size = _fontSize;
            }

            _pixelTexture2DRepository.SetColor(kWindowBackgroundKey, _backgroundColor);
            
            if (_colorPaletteContext is IColorPaletteMutator colorPaletteMutator)
            {
                colorPaletteMutator.SetColor(Constants.ColorPalette.Error, _errorColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.Assert, _assertColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.Warning, _warningColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.Message, _messageColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.Exception, _exceptionColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.Entry, _entryColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.System, _systemColor);

                colorPaletteMutator.SetColor(Constants.ColorPalette.Cursor, _caretColor);
                colorPaletteMutator.SetColor(Constants.ColorPalette.Selection, _selectionColor);
            }

            _promptRenderer.Prompt = _prompt;
            if (_promptStyleContext is IGUIStyleMutator promptStyleMutator)
            {
                promptStyleMutator.SetColor(_promptColor);
            }

            if (_inputStyleContext is IGUIStyleMutator inputStyleMutator)
            {
                inputStyleMutator.SetColor(_inputColor);
            }

            if (_executeButtonsStyleContext is IGUIStyleMutator executeButtonsStyleMutator)
            {
                executeButtonsStyleMutator.SetColor(_executeButtonColor);
            }

            if (_windowControlButtonStyleContext is IGUIStyleMutator windowControlButtonStyleMutator)
            {
                windowControlButtonStyleMutator.SetColor(_windowControlButtonColor);
            }

            if (_logCopyButtonStyleContext is IGUIStyleMutator logCopyButtonStyleMutator)
            {
                logCopyButtonStyleMutator.SetColor(_copyButtonColor);
            }
            
            if (_eventListener.InputHandler != _inputHandler)
            {
                if (_eventListener.InputHandler is IDisposable disposableHandler)
                {
                    disposableHandler.Dispose();
                }

                _eventListener.SetInputHandler(_inputHandler);
            }

            _buttonVisibleContext.IsVisible = _buttonVisible;
            _buttonVisibleContext.IsReverse = _buttonReverse;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            _updatables.Clear();

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; ++i)
            {
                _disposables[i]?.Dispose();
            }

            _disposables.Clear();
        }
    }
}
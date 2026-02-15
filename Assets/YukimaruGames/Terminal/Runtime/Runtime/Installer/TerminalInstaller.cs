using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.Service;
using YukimaruGames.Terminal.Infrastructure;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.SharedKernel.Constants;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.Runtime
{
    public sealed class TerminalInstaller : ITerminalInstaller
    {
        public IReadOnlyList<IUpdatable> Updatables { get; } = new List<IUpdatable>();
        public IReadOnlyList<IRenderer> Renderers { get; } = new List<IRenderer>();
        public IFontConfigurator FontConfigurator => _fontContext;
        public IPixelTexture2DRepository PixelTexture2DRepository => _pixelTexture2DRepository;
        public IColorPaletteConfigurator ColorPaletteConfigurator => _colorPaletteContext;
        public IGUIStyleConfigurator LogStyleConfigurator => _logStyleContext;
        public IGUIStyleConfigurator InputStyleConfigurator => _inputStyleContext;
        public IGUIStyleConfigurator PromptStyleConfigurator => _promptStyleContext;
        public IGUIStyleConfigurator ExecuteButtonStyleConfigurator => _executeButtonsStyleContext;
        public IGUIStyleConfigurator WindowControlButtonStyleConfigurator => _windowControlButtonsStyleContext;
        public IGUIStyleConfigurator LogCopyButtonStyleConfigurator => _logCopyButtonStyleContext;
        public ITerminalWindowAnimatorDataConfigurator WindowAnimatorDataConfigurator { get; }
        public ICursorFlashConfigurator CursorFlashConfigurator => _cursorFlashContext;
        public ITerminalButtonVisibleConfigurator ButtonVisibleConfigurator { get; private set; }
        public ITerminalView View { get; private set; }
        public ITerminalEventListener EventListener { get; private set; }
        public ITerminalService Service { get; private set; }

        private readonly IList<IDisposable> _disposables = new List<IDisposable>();
        private readonly PixelTexture2DRepository _pixelTexture2DRepository = new();
        private TerminalFontContext _fontContext;
        private ColorPaletteContext _colorPaletteContext;
        private TerminalGUIStyleContext _logStyleContext;
        private TerminalGUIStyleContext _inputStyleContext;
        private TerminalGUIStyleContext _promptStyleContext;
        private TerminalGUIStyleContext _executeButtonsStyleContext;
        private TerminalGUIStyleContext _windowControlButtonsStyleContext;
        private TerminalGUIStyleContext _logCopyButtonStyleContext;
        private TerminalWindowAnimatorDataConfigurator _windowAnimatorDataConfigurator;
        private CursorFlashContext _cursorFlashContext;
        private TerminalButtonVisibleContext _buttonVisibleContext;
        private IScrollConfigurator _scrollConfigurator;
        
        public bool Installed { get; private set; }

        public void Install()
        {
            _fontContext = new TerminalFontContext();
            _colorPaletteContext = new ColorPaletteContext(new Dictionary<string, Color>
            {
                { Constants.ColorPalette.Message, Color.white },
                { Constants.ColorPalette.Entry, Color.white },
                { Constants.ColorPalette.Warning, Color.yellow },
                { Constants.ColorPalette.Error, Color.red },
                { Constants.ColorPalette.Assert, Color.red },
                { Constants.ColorPalette.Exception, Color.red },
                { Constants.ColorPalette.System, Color.white },

                { Constants.ColorPalette.Cursor, new(0f, 1f, 0.8f) },
                { Constants.ColorPalette.Selection, new(1f, 0.5f, 0f) },
            });

            _logStyleContext = new TerminalGUIStyleContext(_fontContext);
            _inputStyleContext = new TerminalGUIStyleContext(_fontContext);
            _promptStyleContext = new TerminalGUIStyleContext(_fontContext);
            _executeButtonsStyleContext = new TerminalGUIStyleContext(_fontContext);
            _windowControlButtonsStyleContext = new TerminalGUIStyleContext(_fontContext);
            _logCopyButtonStyleContext = new TerminalGUIStyleContext(_fontContext);
            _cursorFlashContext = new CursorFlashContext(1.886792f);

            var logger = new CommandLogger(256);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var autocomplete = new CommandAutocomplete();
            Service = new TerminalService(
                logger,
                registry,
                invoker,
                parser,
                history,
                autocomplete
            );
            
            // View
            _scrollConfigurator = new ScrollConfigurator();
            var windowRenderer = new TerminalWindowRenderer(PixelTexture2DRepository);
            var logCopyButtonRenderer = new TerminalLogCopyButtonRenderer(_buttonVisibleContext, _logCopyButtonStyleContext);
            var logRenderer = new TerminalLogRenderer(logCopyButtonRenderer, _logStyleContext, _colorPaletteContext);
            var promptRenderer = new TerminalPromptRenderer(_promptStyleContext);
            var inputRenderer = new TerminalInputRenderer(_scrollConfigurator, _promptStyleContext, _colorPaletteContext, _cursorFlashContext);
            var executeButtonRenderer = new TerminalExecuteButtonRenderer(_executeButtonsStyleContext);
            var windowControlButtonRenderer = new TerminalButtonRenderer(PixelTexture2DRepository, _windowControlButtonsStyleContext);
            
            // Presenter
            var windowAnimator = new TerminalWindowAnimator();
            var windowPresenter = new TerminalWindowPresenter(_scrollConfigurator,windowAnimator)

            var logPresenter = new TerminalLogPresenter(Service);
                var inputPresenter=new TerminalInputPresenter()
            View = new TerminalView(
                windowRenderer,
                logRenderer,
                inputRenderer,
                promptRenderer,
                executeButtonRenderer,
                windowControlButtonRenderer,
                logCopyButtonRenderer,
                windowPresenter,
                ,
            );
            Installed = true;
        }

        public void Uninstall()
        {
            
            throw new System.NotImplementedException();
        }
    }
}

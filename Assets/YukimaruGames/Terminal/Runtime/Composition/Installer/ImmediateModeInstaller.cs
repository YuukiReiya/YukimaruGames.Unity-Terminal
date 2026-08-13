using System;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.IMGUI;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Adapters.IMGUI.Coordinators;
using YukimaruGames.Terminal.Adapters.IMGUI.Interfaces.Accessors;
using YukimaruGames.Terminal.Adapters.IMGUI.Renderers;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Infrastructure.Accessors;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Infrastructure.Repositories;
using YukimaruGames.Terminal.Presentation.Accessors;
using YukimaruGames.Terminal.Presentation.Animators;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Interfaces.Repositories;
using YukimaruGames.Terminal.Presentation.Models;
using YukimaruGames.Terminal.Presentation.Presenters;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// Immediate Mode(IMGUI)ベースの標準実装を構築する<see cref="IInstaller"/>実装.
    /// </summary>
    /// <remarks>
    /// Domain層・入力・Scopeの構築やInspector設定の再同期といったバックエンド非依存の処理は
    /// <see cref="InstallerBase"/>にあり、ここではIMGUI固有の描画コンテキストの構築だけを担う(#137).
    /// </remarks>
    [Serializable, AddTypeMenu("IMGUI Installer")]
    public sealed class ImmediateModeInstaller : InstallerBase
    {
        #region runtime-instances

        [NonSerialized] private FontAccessor _fontAccessor;
        [NonSerialized] private IWindowRenderer _windowRenderer;

        [NonSerialized] private IGUIStyleAccessor _logGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _inputGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _promptGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _executeButtonsGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _launcherGUIStyleAccessor;
        [NonSerialized] private IGUIStyleAccessor _logCopyButtonGUIStyleAccessor;

        [NonSerialized] private IPixelTextureRepository _pixelTextureRepository;
        #endregion

        /// <inheritdoc/>
        protected override void ClearReferences()
        {
            _fontAccessor = null;
            _windowRenderer = null;
            _logGUIStyleAccessor = null;
            _inputGUIStyleAccessor = null;
            _promptGUIStyleAccessor = null;
            _executeButtonsGUIStyleAccessor = null;
            _launcherGUIStyleAccessor = null;
            _logCopyButtonGUIStyleAccessor = null;
            _pixelTextureRepository = null;

            base.ClearReferences();
        }

        /// <inheritdoc/>
        protected override void SyncTheme(ITerminalTheme theme)
        {
            base.SyncTheme(theme);

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

            _pixelTextureRepository?.SetColor(Definitions.ThemeLabel.Window, theme.BackgroundColor);
        }

        /// <inheritdoc/>
        protected override RenderingContext BuildRenderingContext(ITerminalTheme theme, ITerminalAnimation animation, ITerminalOptions options, in DomainContext domain)
        {
            var windowAnimationAccessor = CreateWindowAnimationAccessor(animation);
            var colorPaletteAccessor = CreateColorPaletteAccessor(theme);

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

            var cursorFlashSpeedAccessor = CreateCursorFlashSpeedAccessor(theme);
            var launcherVisibleAccessor = CreateLauncherVisibleAccessor(options);

            // Renderers
            _windowRenderer = new WindowRenderer(_pixelTextureRepository);

            var cursorView = new CursorView();
            var logLinePool = new LogLinePool();
            var clipboardRenderer = new ClipboardRenderer(launcherVisibleAccessor, _logCopyButtonGUIStyleAccessor);
            var logRenderer = new LogRenderer(clipboardRenderer, _logGUIStyleAccessor, colorPaletteAccessor, logLinePool);
            var inputRenderer = new InputRenderer(scrollAccessor, _inputGUIStyleAccessor, colorPaletteAccessor, cursorView);
            var promptRenderer = new PromptRenderer(_promptGUIStyleAccessor, domain.Service)
            {
                ShowLoadingIndicator = options.ShowLoadingIndicator,
                LoadingIndicatorFrames = options.LoadingIndicatorFrames,
            };
            PromptRenderer = promptRenderer;
            var executeButtonRenderer = new SubmitRenderer(_executeButtonsGUIStyleAccessor);
            var launcherRenderer = new LauncherRenderer(_pixelTextureRepository, _launcherGUIStyleAccessor);

            // Presenters
            var windowPresenter = new WindowPresenter(
                windowAnimationAccessor,
                new WindowAnimator(),
                new ScreenSizeAccessor(),
                new UnityExceptionLogger());
            var cursorPresenter = new CursorPresenter(cursorFlashSpeedAccessor, cursorView);
            var logPresenter = new LogPresenter(domain.Service);
            var inputPresenter = new InputPresenter(inputRenderer, options.BootupCommand);
            var executeButtonPresenter = new SubmitPresenter(executeButtonRenderer, launcherVisibleAccessor);
            var launcherPresenter = new LauncherPresenter(launcherRenderer, windowPresenter, launcherVisibleAccessor, windowAnimationAccessor);

            // View
            var viewContext = new ViewContext
            {
                WindowRenderer = _windowRenderer,
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

                ScrollAccessor = scrollAccessor,
            };
            var view = new TerminalIMGUI(viewContext);
            var terminalView = new TerminalView(windowPresenter);

            return new RenderingContext
            {
                GUI = view,
                ScrollMutator = scrollAccessor,
                WindowAnimationAccessor = windowAnimationAccessor,
                WindowPresenter = windowPresenter,
                InputPresenter = inputPresenter,
                LogPresenter = logPresenter,
                SubmitPresenter = executeButtonPresenter,
                LauncherPresenter = launcherPresenter,
                View = terminalView,

                Components = new object[]
                {
                    windowAnimationAccessor,
                    colorPaletteAccessor,
                    _fontAccessor,
                    _pixelTextureRepository,
                    scrollAccessor,

                    _logGUIStyleAccessor,
                    _inputGUIStyleAccessor,
                    _promptGUIStyleAccessor,
                    _executeButtonsGUIStyleAccessor,
                    _launcherGUIStyleAccessor,
                    _logCopyButtonGUIStyleAccessor,

                    cursorFlashSpeedAccessor,
                    launcherVisibleAccessor,

                    _windowRenderer,
                    cursorView,
                    logLinePool,
                    clipboardRenderer,
                    logRenderer,
                    inputRenderer,
                    promptRenderer,
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
    }
}

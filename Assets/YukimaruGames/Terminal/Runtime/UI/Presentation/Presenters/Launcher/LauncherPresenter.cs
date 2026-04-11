using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Launcher;
using YukimaruGames.Terminal.Presentation.Presenters.Window;
using YukimaruGames.Terminal.Presentation.Renderers.Launcher;

namespace YukimaruGames.Terminal.Presentation.Presenters.Launcher
{
    public sealed class LauncherPresenter : ILauncherPresenter, ILauncherRenderDataProvider, IDisposable
    {
        private readonly ILauncherRenderer _renderer;
        private readonly IWindowPresenter _windowPresenter;
        private readonly ILauncherVisibleProvider _buttonVisibleProvider;
        private readonly IAnimationDataProvider _animationDataProvider;

        public event Action OnOpenTriggered;
        public event Action OnCloseTriggered;

        public LauncherPresenter(
            ILauncherRenderer renderer,
            IWindowPresenter windowPresenter,
            ILauncherVisibleProvider buttonVisibleProvider,
            IAnimationDataProvider animationDataProvider)
        {
            _renderer = renderer;
            _windowPresenter = windowPresenter;
            _buttonVisibleProvider = buttonVisibleProvider;
            _animationDataProvider = animationDataProvider;

            _renderer.OnClickOpenButton += HandleClickOpenButton;
            _renderer.OnClickCloseButton += HandleClickCloseButton;
        }

        LauncherRenderData ILauncherRenderDataProvider.RenderData=>
            new LauncherRenderData(_buttonVisibleProvider.IsVisible, _buttonVisibleProvider.IsReverse, _windowPresenter.Rect, _animationDataProvider.Anchor);

        private void HandleClickOpenButton() => OnOpenTriggered?.Invoke();
        private void HandleClickCloseButton() => OnCloseTriggered?.Invoke();

        void IDisposable.Dispose()
        {
            _renderer.OnClickOpenButton -= HandleClickOpenButton;
            _renderer.OnClickCloseButton -= HandleClickCloseButton;

            OnOpenTriggered = null;
            OnCloseTriggered = null;
        }
    }
}

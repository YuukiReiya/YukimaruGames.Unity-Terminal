using System;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.UI.Launcher
{
    public sealed class LauncherPresenter : ILauncherPresenter, ILauncherRenderDataProvider, IDisposable
    {
        private readonly ILauncherRenderer _renderer;
        private readonly IWindowPresenter _windowPresenter;
        private readonly ILauncherVisibleProvider _buttonVisibleProvider;

        public event Action OnOpenTriggered;
        public event Action OnCloseTriggered;

        public LauncherPresenter(ILauncherRenderer renderer, IWindowPresenter windowPresenter, ILauncherVisibleProvider buttonVisibleProvider)
        {
            _renderer = renderer;
            _windowPresenter = windowPresenter;
            _buttonVisibleProvider = buttonVisibleProvider;

            _renderer.OnClickOpenButton += HandleClickOpenButton;
            _renderer.OnClickCloseButton += HandleClickCloseButton;
        }

        LauncherRenderData ILauncherRenderDataProvider.GetRenderData()
        {
            return new LauncherRenderData(_buttonVisibleProvider.IsVisible, _buttonVisibleProvider.IsReverse, _windowPresenter.Rect, _windowPresenter.Anchor);
        }

        private void HandleClickOpenButton() => OnOpenTriggered?.Invoke();
        private void HandleClickCloseButton() => OnCloseTriggered?.Invoke();

        public void Dispose()
        {
            _renderer.OnClickOpenButton -= HandleClickOpenButton;
            _renderer.OnClickCloseButton -= HandleClickCloseButton;

            OnOpenTriggered = null;
            OnCloseTriggered = null;
        }
    }
}

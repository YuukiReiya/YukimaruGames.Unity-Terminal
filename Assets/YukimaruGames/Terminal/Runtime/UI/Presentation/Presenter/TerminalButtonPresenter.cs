using System;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalButtonPresenter : ITerminalButtonPresenter, ITerminalButtonRenderDataProvider, IDisposable
    {
        private readonly ITerminalButtonRenderer _renderer;
        private readonly ITerminalWindowPresenter _windowPresenter;
        private readonly ITerminalButtonVisibleProvider _buttonVisibleProvider;

        public event Action OnOpenTriggered;
        public event Action OnCloseTriggered;

        public TerminalButtonPresenter(ITerminalButtonRenderer renderer, ITerminalWindowPresenter windowPresenter, ITerminalButtonVisibleProvider buttonVisibleProvider)
        {
            _renderer = renderer;
            _windowPresenter = windowPresenter;
            _buttonVisibleProvider = buttonVisibleProvider;

            _renderer.OnClickOpenButton += HandleClickOpenButton;
            _renderer.OnClickCloseButton += HandleClickCloseButton;
        }

        TerminalButtonRenderData ITerminalButtonRenderDataProvider.GetRenderData()
        {
            return new TerminalButtonRenderData(_buttonVisibleProvider.IsVisible, _buttonVisibleProvider.IsReverse, _windowPresenter.Rect, _windowPresenter.Anchor);
        }

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

using System;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalCloseButtonPresenter : ITerminalCloseButtonPresenter, ITerminalCloseButtonRenderDataProvider, IDisposable
    {
        private readonly ITerminalCloseButtonRenderer _renderer;
        private readonly ITerminalWindowPresenter _windowPresenter;
        private readonly ITerminalButtonVisibleProvider _buttonVisibleProvider;

        public event Action OnCloseTriggered;

        public TerminalCloseButtonPresenter(ITerminalCloseButtonRenderer renderer,ITerminalWindowPresenter windowPresenter,ITerminalButtonVisibleProvider buttonVisibleProvider)
        {
            _renderer = renderer;
            _windowPresenter = windowPresenter;
            _buttonVisibleProvider = buttonVisibleProvider;

            _renderer.OnClickButton += HandleClickCloseButton;
        }
        
        public TerminalCloseButtonRenderData GetRenderData()
        {
            return new TerminalCloseButtonRenderData(_buttonVisibleProvider.IsVisible, _buttonVisibleProvider.IsReverse, _windowPresenter.Rect, _windowPresenter.Anchor);
        }

        private void HandleClickCloseButton() => OnCloseTriggered?.Invoke();
        
        public void Dispose()
        {
            _renderer.OnClickButton -= HandleClickCloseButton;
            
            OnCloseTriggered = null;
        }

    }
}
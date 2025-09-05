using System;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalOpenButtonPresenter : ITerminalOpenButtonPresenter, ITerminalOpenButtonRenderDataProvider,IDisposable
    {
        private readonly ITerminalOpenButtonRenderer _renderer;
        private readonly ITerminalWindowPresenter _windowPresenter;
        private readonly ITerminalButtonVisibleProvider _buttonVisibleProvider;

        public event Action OnCompactOpenTriggered;
        public event Action OnFullOpenTriggered;
        
        public TerminalOpenButtonPresenter(ITerminalOpenButtonRenderer renderer,ITerminalWindowPresenter windowPresenter, ITerminalButtonVisibleProvider buttonVisibleProvider)
        {
            _renderer = renderer;
            _windowPresenter = windowPresenter;
            _buttonVisibleProvider = buttonVisibleProvider;

            _renderer.OnClickCompactOpenButton += HandleClickCompactOpenButton;
            _renderer.OnClickFullOpenButton += HandleClickFullOpenButton;
        }

        public TerminalOpenButtonRenderData GetRenderData()
        {
            return new TerminalOpenButtonRenderData(_buttonVisibleProvider.IsVisible, _buttonVisibleProvider.IsReverse, _windowPresenter.Rect, _windowPresenter.Anchor);
        }

        private void HandleClickCompactOpenButton() => OnCompactOpenTriggered?.Invoke();
        private void HandleClickFullOpenButton() => OnFullOpenTriggered?.Invoke();

        public void Dispose()
        {
            _renderer.OnClickCompactOpenButton -= HandleClickCompactOpenButton;
            _renderer.OnClickFullOpenButton -= HandleClickFullOpenButton;
            
            OnCompactOpenTriggered = null;
            OnFullOpenTriggered = null;
        }
    }
}

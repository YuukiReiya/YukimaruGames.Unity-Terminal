using System;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalExecuteButtonPresenter : ITerminalExecuteButtonPresenter, IDisposable
    {
        private readonly ITerminalExecuteButtonRenderer _renderer;
        private readonly ITerminalButtonVisibleProvider _buttonVisibleProvider;

        public event Action OnExecuteTriggered;

        public TerminalExecuteButtonPresenter(ITerminalExecuteButtonRenderer renderer, ITerminalButtonVisibleProvider buttonVisibleProvider)
        {
            _renderer = renderer;
            _buttonVisibleProvider = buttonVisibleProvider;

            _renderer.OnClickButton += HandleClickExecuteButton;
        }

        TerminalExecuteButtonRenderData ITerminalExecuteButtonRenderDataProvider.GetRenderData()
        {
            return new TerminalExecuteButtonRenderData(_buttonVisibleProvider.IsVisible);
        }

        private void HandleClickExecuteButton() => OnExecuteTriggered?.Invoke();

        void IDisposable.Dispose()
        {
            _renderer.OnClickButton -= HandleClickExecuteButton;

            OnExecuteTriggered = null;
        }
    }
}

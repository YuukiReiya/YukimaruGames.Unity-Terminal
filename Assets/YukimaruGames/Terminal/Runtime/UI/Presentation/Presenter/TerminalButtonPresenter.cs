using System;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalButtonPresenter : ITerminalButtonPresenter, IDisposable
    {
        private readonly ITerminalButtonRenderer _renderer;
        
        private bool _isVisible;

        public TerminalButtonPresenter(ITerminalButtonRenderer renderer)
        {
            _renderer = renderer;

            _renderer.OnClickExecuteButton += HandleClickExecuteButton;
        }
        
        public TerminalButtonRenderData GetRenderData()
        {
            return new TerminalButtonRenderData(_isVisible);
        }

        public event Action<bool> OnVisibleButtonChanged;
        public event Action OnExecuteTriggered;

        public void SetVisible(bool isVisible)
        {
            if (_isVisible == isVisible)
            {
                return;
            }

            _isVisible = isVisible;
            OnVisibleButtonChanged?.Invoke(isVisible);
        }

        private void HandleClickExecuteButton() => OnExecuteTriggered?.Invoke();

        public void Dispose()
        {
            OnExecuteTriggered = null;
            OnVisibleButtonChanged = null;
        }
    }
}

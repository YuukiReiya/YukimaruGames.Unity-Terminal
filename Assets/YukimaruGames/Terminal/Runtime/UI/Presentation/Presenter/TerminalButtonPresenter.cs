using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalButtonPresenter : ITerminalButtonPresenter, IDisposable
    {
        private readonly ITerminalButtonRenderer _renderer;
        private readonly ITerminalWindowAnimatorDataProvider _provider;
        private readonly ITerminalWindowPresenter _windowPresenter;
        
        private bool _isVisible;
        private Rect _openButtonsRect;

        public TerminalButtonPresenter(ITerminalButtonRenderer renderer, ITerminalWindowAnimatorDataProvider provider,ITerminalWindowPresenter windowPresenter)
        {
            _renderer = renderer;
            _provider = provider;
            _windowPresenter = windowPresenter;

            _renderer.OnClickExecuteButton += HandleClickExecuteButton;
        }

        public TerminalButtonRenderData GetRenderData()
        {
            Calculate();
            return new TerminalButtonRenderData(
                _isVisible,
                _openButtonsRect,
                _provider.Anchor);
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

        private void Calculate()
        {
            var rect = _windowPresenter.Rect;
            switch (_provider.Anchor)
            {
                case TerminalAnchor.Left:
                    _openButtonsRect.x = rect.width;
                    _openButtonsRect.y = 0;
                    break;
                case TerminalAnchor.Right:
                    _openButtonsRect.x =  rect.x;
                    _openButtonsRect.y = rect.height;
                    break;
                case TerminalAnchor.Top:
                    _openButtonsRect.x = 0;
                    _openButtonsRect.y = rect.height;
                    break;
                case TerminalAnchor.Bottom:
                    _openButtonsRect.x = rect.width;
                    _openButtonsRect.y = rect.y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleClickExecuteButton() => OnExecuteTriggered?.Invoke();

        public void Dispose()
        {
            OnExecuteTriggered = null;
            OnVisibleButtonChanged = null;
        }
    }
}

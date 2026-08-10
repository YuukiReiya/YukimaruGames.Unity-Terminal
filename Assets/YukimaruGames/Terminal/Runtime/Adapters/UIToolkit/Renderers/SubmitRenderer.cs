#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Submit;

namespace YukimaruGames.Terminal.Adapters.UIToolkit.Renderers
{
    public sealed class SubmitRenderer : ISubmitRenderer, IDisposable
    {
        private readonly Button _button;

        public string DisplayText => "| exec";

        public event Action OnClickButton;

        public SubmitRenderer(Button button)
        {
            _button = button;

            if (_button == null) return;

            _button.text = DisplayText;
            _button.clicked += HandleClicked;
        }

        public void Render(SubmitRenderData renderData)
        {
            if (_button == null) return;

            _button.style.display = renderData.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void HandleClicked()
        {
            OnClickButton?.Invoke();
        }

        void IDisposable.Dispose()
        {
            if (_button != null)
            {
                _button.clicked -= HandleClicked;
            }

            OnClickButton = null;
        }
    }
}
#endif

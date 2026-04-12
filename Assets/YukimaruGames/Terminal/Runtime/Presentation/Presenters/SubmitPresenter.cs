using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Submit;

namespace YukimaruGames.Terminal.Presentation.Presenters
{
    public sealed class SubmitPresenter : ISubmitPresenter, IDisposable
    {
        private readonly ISubmitRenderer _renderer;
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;

        public event Action OnExecuteTriggered;

        public SubmitPresenter(ISubmitRenderer renderer, ILauncherVisibleProvider launcherVisibleProvider)
        {
            _renderer = renderer;
            _launcherVisibleProvider = launcherVisibleProvider;

            _renderer.OnClickButton += HandleClickExecuteButton;
        }

        SubmitRenderData ISubmitRenderDataProvider.RenderData
            => new SubmitRenderData(_launcherVisibleProvider.IsVisible);

        private void HandleClickExecuteButton() => OnExecuteTriggered?.Invoke();

        void IDisposable.Dispose()
        {
            _renderer.OnClickButton -= HandleClickExecuteButton;

            OnExecuteTriggered = null;
        }
    }
}

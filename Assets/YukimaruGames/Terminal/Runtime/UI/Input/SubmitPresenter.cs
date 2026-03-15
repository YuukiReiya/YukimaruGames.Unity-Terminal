using System;
using YukimaruGames.Terminal.UI.Launcher;

namespace YukimaruGames.Terminal.UI.Input
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

        SubmitRenderData ISubmitRenderDataProvider.GetRenderData()
        {
            return new SubmitRenderData(_launcherVisibleProvider.IsVisible);
        }

        private void HandleClickExecuteButton() => OnExecuteTriggered?.Invoke();

        public void Dispose()
        {
            _renderer.OnClickButton -= HandleClickExecuteButton;

            OnExecuteTriggered = null;
        }
    }
}

using System;
using YukimaruGames.Terminal.UI.Launcher;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public sealed class TerminalExecuteButtonPresenter : ITerminalExecuteButtonPresenter, IDisposable
    {
        private readonly ITerminalExecuteButtonRenderer _renderer;
        private readonly ILauncherVisibleProvider _launcherVisibleProvider;

        public event Action OnExecuteTriggered;

        public TerminalExecuteButtonPresenter(ITerminalExecuteButtonRenderer renderer, ILauncherVisibleProvider launcherVisibleProvider)
        {
            _renderer = renderer;
            _launcherVisibleProvider = launcherVisibleProvider;

            _renderer.OnClickButton += HandleClickExecuteButton;
        }

        TerminalExecuteButtonRenderData ITerminalExecuteButtonRenderDataProvider.GetRenderData()
        {
            return new TerminalExecuteButtonRenderData(_launcherVisibleProvider.IsVisible);
        }

        private void HandleClickExecuteButton() => OnExecuteTriggered?.Invoke();

        public void Dispose()
        {
            _renderer.OnClickButton -= HandleClickExecuteButton;

            OnExecuteTriggered = null;
        }
    }
}

using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;

namespace YukimaruGames.Terminal.Adapters.IMGUI
{
    /// <summary>
    /// <see cref="ITerminalView"/>の実装。<see cref="IWindowPresenter"/>の開閉アニメーションを介して
    /// ウィンドウ全体の表示状態を制御する.
    /// </summary>
    /// <remarks>
    /// Presenterへのフレーム時間注入（<c>Update(float deltaTime)</c>）は
    /// <see cref="SharedKernel.IUpdatable"/>を実装する各Presenterを
    /// <c>Runtime.TerminalEntryPoint</c>が一括で駆動する形で既に実現されているため、
    /// 本クラスはView操作契約（表示/非表示の切り替え）の実装に専念する。
    /// </remarks>
    public sealed class TerminalView : ITerminalView
    {
        private readonly IWindowPresenter _windowPresenter;

        public TerminalView(IWindowPresenter windowPresenter)
        {
            _windowPresenter = windowPresenter;
        }

        void ITerminalView.SetVisible(bool visible)
        {
            if (visible)
            {
                _windowPresenter.Open();
            }
            else
            {
                _windowPresenter.Close();
            }
        }
    }
}

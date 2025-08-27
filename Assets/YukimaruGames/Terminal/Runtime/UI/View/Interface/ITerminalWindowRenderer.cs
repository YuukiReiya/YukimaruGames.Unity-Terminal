using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalWindowRenderer
    {
        void Render(TerminalWindowRenderData viewModel, GUI.WindowFunction func);
    }
}

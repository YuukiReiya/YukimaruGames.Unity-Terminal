using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IWindowRenderer
    {
        void Render(WindowRenderData viewModel, GUI.WindowFunction func);
    }
}

using System;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IWindowRenderer
    {
        void Render(WindowRenderData viewModel, Action<int> func);
    }
}

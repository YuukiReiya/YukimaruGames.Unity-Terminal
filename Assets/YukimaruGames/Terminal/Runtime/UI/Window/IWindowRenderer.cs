using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
{
    public interface IWindowRenderer
    {
        void Render(WindowRenderData viewModel, GUI.WindowFunction func);
    }
}

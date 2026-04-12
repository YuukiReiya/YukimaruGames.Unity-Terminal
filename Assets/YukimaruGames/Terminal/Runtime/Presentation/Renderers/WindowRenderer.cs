using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Interfaces.Repositories;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Renderers
{
    public sealed class WindowRenderer : IWindowRenderer
    {
        private readonly Lazy<GUIStyle> _styleLazy;

        public WindowRenderer(IPixelTextureRepository pixelTextureRepository)
        {
            _styleLazy = new Lazy<GUIStyle>(new GUIStyle()
            {
                normal = new GUIStyleState
                {
                    background = pixelTextureRepository.GetTexture2D(Definitions.ThemeLabel.Window),
                },
            });
        }

        public void Render(WindowRenderData viewModel, GUI.WindowFunction func)
        {
            GUI.Window(viewModel.Id, viewModel.Rect, func, string.Empty, _styleLazy.Value);
        }
    }
}

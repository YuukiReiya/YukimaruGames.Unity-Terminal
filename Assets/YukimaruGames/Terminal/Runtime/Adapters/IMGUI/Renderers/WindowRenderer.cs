using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Interfaces.Repositories;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.IMGUI.Renderers
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

        public void Render(WindowRenderData viewModel, Action<int> func)
        {
            var rect = viewModel.Rect;
            var unityRect = new Rect(rect.X, rect.Y, rect.Width, rect.Height);
            UnityEngine.GUI.Window(viewModel.Id, unityRect, id => func(id), string.Empty, _styleLazy.Value);
        }
    }
}

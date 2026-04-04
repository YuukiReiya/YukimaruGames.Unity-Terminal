using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Core;

namespace YukimaruGames.Terminal.UI.Window
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
                    background = pixelTextureRepository.GetTexture2D(Constants.ThemeLabel.Window),
                },
            });
        }

        public void Render(WindowRenderData viewModel, GUI.WindowFunction func)
        {
            GUI.Window(viewModel.Id, viewModel.Rect, func, string.Empty, _styleLazy.Value);
        }
    }
}

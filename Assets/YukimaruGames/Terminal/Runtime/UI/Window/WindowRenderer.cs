using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Core;

namespace YukimaruGames.Terminal.UI.Window
{
    public sealed class WindowRenderer : IWindowRenderer
    {
        private readonly IPixelTextureRepository _textureRepository;
        private readonly Lazy<GUIStyle> _styleLazy;

        public WindowRenderer(IPixelTextureRepository pixelTextureRepository)
        {
            _textureRepository = pixelTextureRepository;
            _textureRepository.SetColor(Constants.ColorPalette.Window, Color.black);

            _styleLazy = new Lazy<GUIStyle>(new GUIStyle()
            {
                normal = new GUIStyleState
                {
                    background = pixelTextureRepository.GetTexture2D(Constants.ColorPalette.Window),
                },
            });
        }

        public void Render(WindowRenderData viewModel, GUI.WindowFunction func)
        {
            GUI.Window(viewModel.Id, viewModel.Rect, func, string.Empty, _styleLazy.Value);
        }
    }
}

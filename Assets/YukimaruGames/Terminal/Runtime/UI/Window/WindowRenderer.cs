using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.UI.Window
{
    public sealed class WindowRenderer : IWindowRenderer
    {
        private readonly IPixelTexture2DRepository _textureRepository;
        
        private const string Key = "WindowGUIStyle";

        private readonly Lazy<GUIStyle> _styleLazy;

        public WindowRenderer(IPixelTexture2DRepository pixelTextureRepository)
        {
            _textureRepository = pixelTextureRepository;
            _textureRepository.SetColor(Key, Color.black);

            _styleLazy = new Lazy<GUIStyle>(new GUIStyle()
            {
                normal = new GUIStyleState
                {
                    background = pixelTextureRepository.GetTexture2D(Key),
                },
            });
        }

        public void SetBackgroundColor(Color color)
        {
            _textureRepository.SetColor(Key, color);
        }

        public void Render(WindowRenderData viewModel, GUI.WindowFunction func)
        {
            GUI.Window(viewModel.Id, viewModel.Rect, func, string.Empty, _styleLazy.Value);
        }
    }
}

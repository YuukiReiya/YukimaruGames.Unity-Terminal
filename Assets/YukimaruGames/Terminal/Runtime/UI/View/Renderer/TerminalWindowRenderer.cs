using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalWindowRenderer : ITerminalWindowRenderer
    {
        private readonly IPixelTexture2DRepository _textureRepository;
        
        private const string Key = "WindowGUIStyle";

        private readonly Lazy<GUIStyle> _styleLazy;

        public TerminalWindowRenderer(IPixelTexture2DRepository pixelTextureRepository)
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

        public void Render(TerminalWindowRenderData viewModel, GUI.WindowFunction func)
        {
            GUI.Window(viewModel.Id, viewModel.Rect, func, string.Empty, _styleLazy.Value);
        }
    }
}

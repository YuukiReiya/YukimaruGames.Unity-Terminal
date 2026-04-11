using System;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Renderers
{
    public interface IClipboardRenderer
    {
        event Action<string> OnClickButton;
        void Render(string copyText);
    }
}

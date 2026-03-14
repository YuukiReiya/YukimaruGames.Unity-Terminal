using System;

namespace YukimaruGames.Terminal.UI.Clipboard
{
    public interface IClipboardRenderer
    {
        event Action<string> OnClickButton;
        void Render(string copyText);
    }
}

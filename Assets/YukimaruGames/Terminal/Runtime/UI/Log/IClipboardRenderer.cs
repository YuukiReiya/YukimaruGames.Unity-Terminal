using System;

namespace YukimaruGames.Terminal.UI.Log
{
    public interface IClipboardRenderer
    {
        event Action<string> OnClickButton;
        void Render(string copyText);
    }
}

using System;

namespace YukimaruGames.Terminal.UI.View
{
    public interface ITerminalLogCopyButtonRenderer
    {
        event Action<string> OnClickButton;
        void Render(string copyText);
    }
}

using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Animators
{
    public interface IWindowAnimator
    {
        TerminalRect Evaluate(WindowAnimatorData data);
    }
}

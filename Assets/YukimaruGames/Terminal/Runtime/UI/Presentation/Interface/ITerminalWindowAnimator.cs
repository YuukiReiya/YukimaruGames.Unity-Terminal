using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation.Model;

namespace YukimaruGames.Terminal.UI.Presentation
{
    public interface ITerminalWindowAnimator
    {
        Rect Evaluate(TerminalWindowAnimatorData data);
    }
}

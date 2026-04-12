using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Animators
{
    public interface IWindowAnimator
    {
        Rect Evaluate(WindowAnimatorData data);
    }
}

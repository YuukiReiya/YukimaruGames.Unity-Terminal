using UnityEngine;

namespace YukimaruGames.Terminal.UI.Window
{
    public interface IWindowAnimator
    {
        Rect Evaluate(WindowAnimatorData data);
    }
}

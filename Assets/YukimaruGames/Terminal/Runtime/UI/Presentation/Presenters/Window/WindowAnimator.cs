using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Presenters.Window
{
    public sealed class WindowAnimator : IWindowAnimator
    {
        public Rect Evaluate(WindowAnimatorData data)
        {
            if (Mathf.Approximately(0f, data.Duration))
            {
                return Calculate(data, 1f);
            }
            
            var t = Mathf.Clamp01(data.Elapsed / data.Duration);
            var step = Mathf.SmoothStep(0f, 1f, t);

            return Calculate(data, step);
        }

        private Rect Calculate(in WindowAnimatorData data, float step)
        {
            step = data.State switch
            {
                WindowState.Open => step,
                WindowState.Close => Mathf.Clamp01(1f - step),
                _ => throw new ArgumentOutOfRangeException()
            };
            var scale = data.Style switch
            {
                WindowStyle.Compact => Mathf.Clamp01(data.Scale),
                WindowStyle.Full => 1f,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var rect = new Rect();

            var screen = data.Size;
            
#pragma warning disable CS8509
            switch (data.Anchor)
            {
                case WindowAnchor.Left:
                case WindowAnchor.Right:
                    rect.height = screen.height;
                    rect.width = screen.width * Mathf.Clamp01(scale);

                    rect.x = data.Anchor switch
                    {
                        WindowAnchor.Left => -rect.width + rect.width * Mathf.Clamp01(step),
                        WindowAnchor.Right => screen.width - rect.width * Mathf.Clamp01(step),
                    };
                    break;
                case WindowAnchor.Top:
                case WindowAnchor.Bottom:
                    rect.height = screen.height * Mathf.Clamp01(scale);
                    rect.width = screen.width;

                    rect.y = data.Anchor switch
                    {
                        WindowAnchor.Top => -rect.height + rect.height * Mathf.Clamp01(step),
                        WindowAnchor.Bottom => screen.height - rect.height * Mathf.Clamp01(step),
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#pragma warning restore CS8509

            return rect;
        }
    }
}

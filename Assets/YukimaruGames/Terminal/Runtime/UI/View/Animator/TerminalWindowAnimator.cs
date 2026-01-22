using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.UI.View
{
    public sealed class TerminalWindowAnimator : ITerminalWindowAnimator
    {
        public Rect Evaluate(TerminalWindowAnimatorData data)
        {
            if (Mathf.Approximately(0f, data.Duration))
            {
                return Calculate(data, 1f);
            }
            
            var t = Mathf.Clamp01(data.Elapsed / data.Duration);
            var step = Mathf.SmoothStep(0f, 1f, t);

            return Calculate(data, step);
        }

        private Rect Calculate(in TerminalWindowAnimatorData data, float step)
        {
            step = data.State switch
            {
                TerminalState.Open => step,
                TerminalState.Close => Mathf.Clamp01(1f - step),
                _ => throw new ArgumentOutOfRangeException()
            };
            var scale = data.Style switch
            {
                TerminalWindowStyle.Compact => Mathf.Clamp01(data.Scale),
                TerminalWindowStyle.Full => 1f,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            var rect = new Rect();

            var screen = data.Size;
            
#pragma warning disable CS8509
            switch (data.Anchor)
            {
                case TerminalAnchor.Left:
                case TerminalAnchor.Right:
                    rect.height = screen.height;
                    rect.width = screen.width * Mathf.Clamp01(scale);

                    rect.x = data.Anchor switch
                    {
                        TerminalAnchor.Left => -rect.width + rect.width * Mathf.Clamp01(step),
                        TerminalAnchor.Right => screen.width - rect.width * Mathf.Clamp01(step),
                    };
                    break;
                case TerminalAnchor.Top:
                case TerminalAnchor.Bottom:
                    rect.height = screen.height * Mathf.Clamp01(scale);
                    rect.width = screen.width;

                    rect.y = data.Anchor switch
                    {
                        TerminalAnchor.Top => -rect.height + rect.height * Mathf.Clamp01(step),
                        TerminalAnchor.Bottom => screen.height - rect.height * Mathf.Clamp01(step),
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

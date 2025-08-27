using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;
using YukimaruGames.Terminal.UI.Presentation.Model;
using YukimaruGames.Terminal.UI.View.Model;

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
            var screen = (Screen.width, Screen.height);
            
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

            switch (data.Anchor)
            {
                case TerminalAnchor.Left:
                case TerminalAnchor.Right:
                    rect.height = screen.height;
                    rect.width = screen.width * Mathf.Clamp01(scale) * Mathf.Clamp01(step);
                    if (data.Anchor is TerminalAnchor.Right)
                    {
                        rect.x = screen.width - rect.width;
                    }
                    break;
                case TerminalAnchor.Top:
                case TerminalAnchor.Bottom:
                    rect.height = screen.height * scale * Mathf.Clamp01(step);
                    rect.width = screen.width;
                    if (data.Anchor is TerminalAnchor.Bottom)
                    {
                        rect.y = screen.height - rect.height;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return rect;
        }
    }
}

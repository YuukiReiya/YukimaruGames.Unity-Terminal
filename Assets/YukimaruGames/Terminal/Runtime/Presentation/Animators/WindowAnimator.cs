using System;
using YukimaruGames.Terminal.Domain.Models;
using YukimaruGames.Terminal.Presentation.Interfaces.Animators;
using YukimaruGames.Terminal.Presentation.Models.Window;
using YukimaruGames.Terminal.SharedKernel.Mathematics;

namespace YukimaruGames.Terminal.Presentation.Animators
{
    public sealed class WindowAnimator : IWindowAnimator
    {
        public TerminalRect Evaluate(WindowAnimatorData data)
        {
            if (TerminalMath.Approximately(0f, data.Duration))
            {
                return Calculate(data, 1f);
            }

            var t = TerminalMath.Clamp01(data.Elapsed / data.Duration);
            var step = TerminalMath.SmoothStep(0f, 1f, t);

            return Calculate(data, step);
        }

        private TerminalRect Calculate(in WindowAnimatorData data, float step)
        {
            step = data.State switch
            {
                WindowState.Open => step,
                WindowState.Close => TerminalMath.Clamp01(1f - step),
                _ => throw new ArgumentOutOfRangeException()
            };
            var scale = data.Style switch
            {
                WindowStyle.Compact => TerminalMath.Clamp01(data.Scale),
                WindowStyle.Full => 1f,
                _ => throw new ArgumentOutOfRangeException()
            };

            float x = 0f, y = 0f, width = 0f, height = 0f;

            var screen = data.Size;

#pragma warning disable CS8509
            switch (data.Anchor)
            {
                case WindowAnchor.Left:
                case WindowAnchor.Right:
                    height = screen.height;
                    width = screen.width * TerminalMath.Clamp01(scale);

                    x = data.Anchor switch
                    {
                        WindowAnchor.Left => -width + width * TerminalMath.Clamp01(step),
                        WindowAnchor.Right => screen.width - width * TerminalMath.Clamp01(step),
                    };
                    break;
                case WindowAnchor.Top:
                case WindowAnchor.Bottom:
                    height = screen.height * TerminalMath.Clamp01(scale);
                    width = screen.width;

                    y = data.Anchor switch
                    {
                        WindowAnchor.Top => -height + height * TerminalMath.Clamp01(step),
                        WindowAnchor.Bottom => screen.height - height * TerminalMath.Clamp01(step),
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
#pragma warning restore CS8509

            return new TerminalRect(x, y, width, height);
        }
    }
}

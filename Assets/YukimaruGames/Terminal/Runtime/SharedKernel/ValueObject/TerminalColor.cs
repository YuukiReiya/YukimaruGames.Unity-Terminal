using System;

namespace YukimaruGames.Terminal.SharedKernel
{
    /// <summary>
    /// エンジンに依存しない色の値オブジェクトです。
    /// </summary>
    [Serializable]
    public readonly struct TerminalColor : IEquatable<TerminalColor>
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public TerminalColor(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public bool Equals(TerminalColor other)
        {
            return R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);
        }

        public override bool Equals(object obj)
        {
            return obj is TerminalColor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }

        public static bool operator ==(TerminalColor left, TerminalColor right) => left.Equals(right);
        public static bool operator !=(TerminalColor left, TerminalColor right) => !left.Equals(right);
    }
}

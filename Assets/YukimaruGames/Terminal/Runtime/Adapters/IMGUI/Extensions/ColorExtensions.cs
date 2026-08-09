using UnityEngine;
using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Adapters.IMGUI.Extensions
{
    /// <summary>
    /// <see cref="TerminalColor"/>と<see cref="Color"/>を相互変換する拡張メソッド.
    /// <para>
    /// 両者ともGamma色空間（0-1のfloatまたは0-255のbyte）の値として扱われるため、
    /// 単純な正規化のみで変換する（Linear/Gamma変換は行わない）。
    /// </para>
    /// </summary>
    public static class ColorExtensions
    {
        private const float MaxByteValue = byte.MaxValue;
        private const float RoundingOffset = 0.5f;

        /// <summary>
        /// <see cref="TerminalColor"/>を<see cref="Color"/>へ変換する.
        /// </summary>
        public static Color ToUnityColor(this TerminalColor color)
        {
            return new Color(
                color.R / MaxByteValue,
                color.G / MaxByteValue,
                color.B / MaxByteValue,
                color.A / MaxByteValue);
        }

        /// <summary>
        /// <see cref="Color"/>を<see cref="TerminalColor"/>へ変換する.
        /// </summary>
        public static TerminalColor ToTerminalColor(this Color color)
        {
            return new TerminalColor(
                ToColorByte(color.r),
                ToColorByte(color.g),
                ToColorByte(color.b),
                ToColorByte(color.a));
        }

        private static byte ToColorByte(float value)
        {
            var clamped = Mathf.Clamp01(value);
            return (byte)(clamped * MaxByteValue + RoundingOffset);
        }
    }
}

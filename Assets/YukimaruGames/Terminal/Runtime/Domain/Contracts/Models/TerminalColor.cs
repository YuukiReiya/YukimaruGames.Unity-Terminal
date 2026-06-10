using System;
using System.Globalization;

namespace YukimaruGames.Terminal.Domain.Models
{
    /// <summary>
    /// ターミナルシステムで使用するカラー値型.
    /// <para>
    /// RGBA（赤、緑、青、アルファ）カラー情報を保持します。
    /// Gamma色空間（UI表示用）で動作します。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>色空間について:</b>
    /// TerminalColorはGamma色空間で動作し、UI表示用に最適化されています。
    /// 線形計算が必要な場合は、事前に ToLinear() で変換してください。
    /// </para>
    /// <para>
    /// <b>使用例:</b>
    /// <code>
    /// // リテラル値で作成
    /// var red = new TerminalColor(255, 0, 0);
    /// var whiteTransparent = new TerminalColor(255, 255, 255, 128);
    /// 
    /// // HTML形式で作成
    /// TerminalColor.TryParseHex("#FF0000", out var red);
    /// 
    /// // 標準カラー
    /// var black = TerminalColor.Black;
    /// var white = TerminalColor.White;
    /// </code>
    /// </para>
    /// </remarks>
    public readonly struct TerminalColor : IEquatable<TerminalColor>
    {
        /// <summary>
        /// 赤成分（0-255）.
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// 緑成分（0-255）.
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// 青成分（0-255）.
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// アルファ成分（0-255、0=透明、255=不透明）.
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="r">赤成分（0-255）</param>
        /// <param name="g">緑成分（0-255）</param>
        /// <param name="b">青成分（0-255）</param>
        /// <param name="a">アルファ成分（デフォルト: 255）</param>
        public TerminalColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// ARGB形式の整数値（0xAARRGGBB）からカラーを作成.
        /// </summary>
        /// <param name="argb">ARGB形式の32ビット整数</param>
        public TerminalColor(uint argb)
        {
            A = (byte)((argb >> 24) & 0xFF);
            R = (byte)((argb >> 16) & 0xFF);
            G = (byte)((argb >> 8) & 0xFF);
            B = (byte)(argb & 0xFF);
        }

        /// <summary>
        /// ARGB形式の整数値を取得（0xAARRGGBB）.
        /// </summary>
        public uint ToArgb()
        {
            return ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;
        }

        /// <summary>
        /// HTML形式（#RRGGBB または #RRGGBBAA）からカラーをパースする.
        /// </summary>
        /// <param name="hex">HTML形式のカラーコード（例: "#FF0000", "#FF0000FF"）</param>
        /// <param name="color">パース結果</param>
        /// <returns>パースの成功可否</returns>
        /// <example>
        /// <code>
        /// if (TerminalColor.TryParseHex("#FF0000", out var red))
        /// {
        ///     // red = (255, 0, 0, 255)
        /// }
        /// 
        /// if (TerminalColor.TryParseHex("#FF000080", out var redTransparent))
        /// {
        ///     // redTransparent = (255, 0, 0, 128)
        /// }
        /// </code>
        /// </example>
        public static bool TryParseHex(string hex, out TerminalColor color)
        {
            color = default;

            if (string.IsNullOrEmpty(hex))
                return false;

            // # を削除
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);

            // 6文字（RGB）または 8文字（RGBA）
            if (hex.Length != 6 && hex.Length != 8)
                return false;

            try
            {
                uint r = uint.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                uint g = uint.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                uint b = uint.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                uint a = hex.Length == 8
                    ? uint.Parse(hex.Substring(6, 2), NumberStyles.HexNumber)
                    : 255U;

                color = new TerminalColor((byte)r, (byte)g, (byte)b, (byte)a);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// HTML形式（#RRGGBB または #RRGGBBAA）のカラーコードを取得.
        /// </summary>
        /// <param name="includeAlpha">アルファ値を含めるかどうか</param>
        /// <returns>HTML形式のカラーコード</returns>
        /// <example>
        /// <code>
        /// var red = new TerminalColor(255, 0, 0);
        /// var hex = red.ToHex();  // "#FF0000"
        /// 
        /// var redTransparent = new TerminalColor(255, 0, 0, 128);
        /// var hexAlpha = redTransparent.ToHex(true);  // "#FF000080"
        /// </code>
        /// </example>
        public string ToHex(bool includeAlpha = false)
        {
            if (includeAlpha)
                return $"#{R:X2}{G:X2}{B:X2}{A:X2}";
            else
                return $"#{R:X2}{G:X2}{B:X2}";
        }

        /// <summary>
        /// 線形色空間（Linear）に変換する.
        /// <para>
        /// Gamma = 2.2 を使用して線形化します。
        /// </para>
        /// </summary>
        /// <returns>線形色空間のカラー（浮動小数点、0-1範囲）</returns>
        public (float r, float g, float b, float a) ToLinear()
        {
            const float gamma = 2.2f;
            float r = MathF.Pow(R / 255f, gamma);
            float g = MathF.Pow(G / 255f, gamma);
            float b = MathF.Pow(B / 255f, gamma);
            float a = A / 255f;
            return (r, g, b, a);
        }

        /// <summary>
        /// 線形色空間（Linear）のカラーからGamma色空間に変換する.
        /// <para>
        /// Gamma = 2.2 を使用してガンマ補正します。
        /// </para>
        /// </summary>
        /// <param name="linear">線形色空間のカラー（0-1範囲）</param>
        /// <returns>Gamma色空間のカラー</returns>
        public static TerminalColor FromLinear(float r, float g, float b, float a = 1f)
        {
            const float gamma = 1f / 2.2f;
            byte rb = (byte)(MathF.Pow(r, gamma) * 255f);
            byte gb = (byte)(MathF.Pow(g, gamma) * 255f);
            byte bb = (byte)(MathF.Pow(b, gamma) * 255f);
            byte ab = (byte)(a * 255f);
            return new TerminalColor(rb, gb, bb, ab);
        }

        /// <summary>
        /// 等価性を判定.
        /// </summary>
        public bool Equals(TerminalColor other) =>
            R == other.R && G == other.G && B == other.B && A == other.A;

        /// <summary>
        /// 等価性を判定.
        /// </summary>
        public override bool Equals(object obj) =>
            obj is TerminalColor other && Equals(other);

        /// <summary>
        /// ハッシュコードを取得.
        /// </summary>
        public override int GetHashCode() =>
            unchecked((int)ToArgb());

        /// <summary>
        /// 文字列表現を取得.
        /// </summary>
        public override string ToString() =>
            $"TerminalColor({R}, {G}, {B}, {A})";

        /// <summary>
        /// 等価演算子.
        /// </summary>
        public static bool operator ==(TerminalColor a, TerminalColor b) =>
            a.Equals(b);

        /// <summary>
        /// 非等価演算子.
        /// </summary>
        public static bool operator !=(TerminalColor a, TerminalColor b) =>
            !a.Equals(b);

        // ─── 標準カラー定数 ────────────────────────────────────────────────────

        /// <summary>黒（#000000）</summary>
        public static readonly TerminalColor Black = new(0, 0, 0);

        /// <summary>白（#FFFFFF）</summary>
        public static readonly TerminalColor White = new(255, 255, 255);

        /// <summary>赤（#FF0000）</summary>
        public static readonly TerminalColor Red = new(255, 0, 0);

        /// <summary>緑（#00FF00）</summary>
        public static readonly TerminalColor Green = new(0, 255, 0);

        /// <summary>青（#0000FF）</summary>
        public static readonly TerminalColor Blue = new(0, 0, 255);

        /// <summary>黄（#FFFF00）</summary>
        public static readonly TerminalColor Yellow = new(255, 255, 0);

        /// <summary>シアン（#00FFFF）</summary>
        public static readonly TerminalColor Cyan = new(0, 255, 255);

        /// <summary>マゼンタ（#FF00FF）</summary>
        public static readonly TerminalColor Magenta = new(255, 0, 255);

        /// <summary>灰色（#808080）</summary>
        public static readonly TerminalColor Gray = new(128, 128, 128);

        /// <summary>透明（#00000000）</summary>
        public static readonly TerminalColor Transparent = new(0, 0, 0, 0);
    }
}
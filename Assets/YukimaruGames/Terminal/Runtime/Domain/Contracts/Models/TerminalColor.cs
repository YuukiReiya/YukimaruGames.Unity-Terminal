using System;
using System.Globalization;
using System.Runtime.CompilerServices;

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
        /// この <see cref="TerminalColor"/> を16進数文字列（#RRGGBB または #RRGGBBAA）としてフォーマットし、指定されたバッファに書き込みます。
        /// </summary>
        /// <param name="destination">書き込み先の文字バッファ。</param>
        /// <param name="includeAlpha">アルファ成分を含める場合は <c>true</c>。このとき最低 9 文字の領域が必要です。</param>
        /// <returns>
        /// 書き込みに成功した場合は <c>true</c>。
        /// <paramref name="destination"/> のサイズが不足していて書き込めなかった場合は <c>false</c> を返します。
        /// </returns>
        /// <remarks>
        /// このメソッドは GC アロケーションを一切発生させません（Zero-Allocation）。
        /// <para>
        /// 必要となる最小バッファサイズは以下の通りです：
        /// <list type="bullet">
        /// <item><description><paramref name="includeAlpha"/> が <c>false</c> の場合: 7 文字</description></item>
        /// <item><description><paramref name="includeAlpha"/> が <c>true</c> の場合: 9 文字</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public bool TryFormat(Span<char> destination, bool includeAlpha)
        {
            var requiredLength = includeAlpha ?
                9 :
                7;

            if (destination.Length < requiredLength)
            {
                return false;
            }

            destination[0] = '#';

            if (!R.TryFormat(destination.Slice(1, 2), out _, "X2") ||
                !G.TryFormat(destination.Slice(3, 2), out _, "X2") ||
                !B.TryFormat(destination.Slice(5, 2), out _, "X2"))
            {
                return false;
            }

            if (includeAlpha)
            {
                if (!A.TryFormat(destination.Slice(7, 2), out _, "X2"))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// HTML形式（#RRGGBB または #RRGGBBAA）からカラーをパースする.
        /// </summary>
        /// <param name="hexSpan">HTML形式のカラーコードSpan表現（例: "#FF0000", "#FF0000FF"）</param>
        /// <param name="color">パース結果</param>
        /// <returns>パースの成功可否</returns>
        /// <example>
        /// <code>
        /// // 1. 標準的な6桁形式（RGB: Alphaは自動的に255に設定されます）
        /// if (TerminalColor.TryParseHex("#FF0000", out var red))
        /// {
        ///     // red.R = 255, red.G = 0, red.B = 0, red.A = 255
        /// }
        /// 
        /// // 2. アルファ値を含む8桁形式（RGBA）
        /// if (TerminalColor.TryParseHex("#FF000080", out var redTransparent))
        /// {
        ///     // redTransparent.R = 255, redTransparent.G = 0, redTransparent.B = 0, redTransparent.A = 128
        /// }
        /// 
        /// // 3. ゼロアロケーション最適化（ReadOnlySpan&lt;char&gt; による部分文字列パース）
        /// // 文字列の切り出し（Substring）による GC Alloc を発生させずに安全にパース可能です
        /// ReadOnlySpan&lt;char&gt; logLine = "#00FF00[Update]".AsSpan();
        /// if (TerminalColor.TryParseHex(logLine.Slice(0, 7), out var green))
        /// {
        ///     // green.R = 0, green.G = 255, green.B = 0, green.A = 255
        /// }
        /// </code>
        /// </example>
        public static bool TryParseHex(ReadOnlySpan<char> hexSpan, out TerminalColor color)
        {
            color = default;

            if (hexSpan.IsEmpty)
            {
                return false;
            }

            // # を削除
            if (hexSpan[0] == '#')
            {
                hexSpan = hexSpan.Slice(1);
            }

            // 6文字（RGB）または 8文字（RGBA）
            if (hexSpan.Length is not (6 or 8))
            {
                return false;
            }

            if (!byte.TryParse(hexSpan.Slice(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !byte.TryParse(hexSpan.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !byte.TryParse(hexSpan.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return false;
            }

            byte a = 255;
            if (hexSpan.Length == 8)
            {
                if (!byte.TryParse(hexSpan.Slice(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
                {
                    return false;
                }
            }

            color = new TerminalColor(r, g, b, a);
            return true;
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
        /// </summary>
        /// <remarks>
        /// <para>
        /// Gamma = 2.2 を使用してガンマ補正します。
        /// </para>
        /// 線形色空間のカラー（0-1範囲）
        /// </remarks>
        /// <returns>Gamma色空間のカラー</returns>
        public static TerminalColor FromLinear(float r, float g, float b, float a = 1f)
        {
            const float gamma = 1f / 2.2f;
            return new TerminalColor(
                ToColorByte(r, gamma),
                ToColorByte(g, gamma),
                ToColorByte(b, gamma),
                ToColorByte(a, gamma));
        }

        /// <summary>
        /// 正規化された浮動小数点値 (0.0 ～ 1.0) を、ガンマ補正を適用した上で 8bit 色成分 (0 ～ 255) に変換します。
        /// </summary>
        /// <param name="value">変換元の値 (0.0 未満または 1.0 を超える場合は内部でクランプされます)。</param>
        /// <param name="gamma">ガンマ補正の指数値。</param>
        /// <returns>変換後の 8bit 符号なし整数 (0 ～ 255)。</returns>
        /// <remarks>
        /// <para>
        /// このメソッドは GC アロケーションを一切発生させず、非常に高速に動作するように設計されています。
        /// </para>
        /// <para>
        /// 四捨五入には <see cref="MathF.Round(float)"/> 等のメソッド呼び出しを行わず、最適化されたイディオム (+ 0.5f) を
        /// 採用しており、パフォーマンスのオーバーヘッドを極限まで排除しています。
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ToColorByte(float value, float gamma)
        {
            var clamped = MathF.Min(MathF.Max(0f, value), 1f);
            var powered = MathF.Pow(clamped, gamma);

            const float maxByteValue = byte.MaxValue;
            const float roundingOffset = 0.5f;
            
            return (byte)(powered * maxByteValue + roundingOffset);
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
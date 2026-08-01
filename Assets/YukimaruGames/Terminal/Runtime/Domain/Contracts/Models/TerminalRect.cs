using System;

namespace YukimaruGames.Terminal.Domain.Models
{
    /// <summary>
    /// ターミナルシステムで使用する矩形領域の値型.
    /// <para>
    /// UnityEngine.Rect の代替として、Presentation層で使用可能な非Unity依存の矩形を表します。
    /// </para>
    /// </summary>
    public readonly struct TerminalRect : IEquatable<TerminalRect>
    {
        /// <summary>左上X座標.</summary>
        public float X { get; }

        /// <summary>左上Y座標.</summary>
        public float Y { get; }

        /// <summary>幅.</summary>
        public float Width { get; }

        /// <summary>高さ.</summary>
        public float Height { get; }

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        public TerminalRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 等価性を判定.
        /// </summary>
        public bool Equals(TerminalRect other) =>
            X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);

        /// <summary>
        /// 等価性を判定.
        /// </summary>
        public override bool Equals(object obj) =>
            obj is TerminalRect other && Equals(other);

        /// <summary>
        /// ハッシュコードを取得.
        /// </summary>
        public override int GetHashCode() =>
            HashCode.Combine(X, Y, Width, Height);

        /// <summary>
        /// 文字列表現を取得.
        /// </summary>
        public override string ToString() =>
            $"TerminalRect({X}, {Y}, {Width}, {Height})";

        /// <summary>
        /// 等価演算子.
        /// </summary>
        public static bool operator ==(TerminalRect a, TerminalRect b) => a.Equals(b);

        /// <summary>
        /// 非等価演算子.
        /// </summary>
        public static bool operator !=(TerminalRect a, TerminalRect b) => !a.Equals(b);

        /// <summary>サイズ0の矩形.</summary>
        public static readonly TerminalRect Zero = new(0f, 0f, 0f, 0f);
    }
}

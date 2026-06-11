using System;

namespace YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects
{
    /// <summary>
    /// コマンドの引数型.
    /// </summary>
    public readonly struct CommandArgument : IEquatable<CommandArgument>
    {
        private readonly ReadOnlyMemory<char> _value;

        /// <summary>
        /// 文字列としての引数.
        /// </summary>
        public string String => _value.ToString();

        #region 整数型

        /// <summary>
        /// 符号付き8ビット
        /// </summary>
        /// <remarks>
        /// -128 ～ 127
        /// </remarks>
        public sbyte SByte => sbyte.Parse(_value.Span);

        /// <summary>
        /// 符号なし8ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 255
        /// </remarks>
        public byte Byte => byte.Parse(_value.Span);

        /// <summary>
        /// 符号付き16ビット
        /// </summary>
        /// <remarks>
        /// -32,768 ～ 32,767
        /// </remarks>
        public short Short => short.Parse(_value.Span);

        /// <summary>
        /// 符号なし16ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 65,535
        /// </remarks>
        public ushort UShort => ushort.Parse(_value.Span);

        /// <summary>
        /// 符号付き32ビット
        /// </summary>
        /// <remarks>
        /// -2,147,483,648 ～ 2,147,483,647
        /// </remarks>
        public int Int => int.Parse(_value.Span);

        /// <summary>
        /// 符号なし32ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 4,294,967,295
        /// </remarks>
        public uint UInt => uint.Parse(_value.Span);

        /// <summary>
        /// 符号付き64ビット
        /// </summary>
        /// <remarks>
        /// -9,223,372,036,854,775,808 ～ 9,223,372,036,854,775,807
        /// </remarks>
        public long Long => long.Parse(_value.Span);

        /// <summary>
        /// 符号なし64ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 18,446,744,073,709,551,615
        /// </remarks>
        public ulong ULong => ulong.Parse(_value.Span);

        #endregion

        #region 浮動小数点型

        /// <summary>
        /// 4バイト
        /// </summary>
        /// <remarks>
        ///	有効桁数　: ~6 ～9 桁
        /// ±1.5 x 10−45 から ±3.4 x 1038
        /// </remarks>
        public float Float => float.Parse(_value.Span);

        /// <summary>
        /// 8バイト
        /// </summary>
        /// <remarks>
        /// 有効桁数　:　~15-17 桁
        /// ±5.0 × 10−324 - ±1.7 × 10308
        /// </remarks>
        public double Double => double.Parse(_value.Span);

        /// <summary>
        /// 16バイト
        /// </summary>
        /// <remarks>
        /// 有効桁数　:　28 から 29 桁の数字
        /// ±1.0 x 10-28 から ±7.9228 x 1028
        /// </remarks>>
        public decimal Decimal => decimal.Parse(_value.Span);

        #endregion

        /// <summary>
        /// bool型への変換
        /// </summary>
        /// <remarks>
        /// 大文字/小文字の判定は無視.
        /// </remarks>
        public bool Bool => bool.Parse(_value.Span);

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="argument">引数</param>
        public CommandArgument(string argument)
        {
            _value = (argument ?? throw new ArgumentNullException(nameof(argument))).AsMemory();
        }

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="argument">引数</param>
        public CommandArgument(ReadOnlyMemory<char> argument)
        {
            _value = argument;
        }

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="argument">引数</param>
        public CommandArgument(ReadOnlySpan<char> argument)
        {
            _value = argument.ToArray();
        }

        public T As<T>()
        {
            return typeof(T) switch
            {
                var t when t == typeof(CommandArgument) => (T)(object)this,
                var t when t == typeof(sbyte)           => (T)(object)SByte,
                var t when t == typeof(byte)            => (T)(object)Byte,
                var t when t == typeof(short)           => (T)(object)Short,
                var t when t == typeof(ushort)          => (T)(object)UShort,
                var t when t == typeof(int)             => (T)(object)Int,
                var t when t == typeof(uint)            => (T)(object)UInt,
                var t when t == typeof(long)            => (T)(object)Long,
                var t when t == typeof(ulong)           => (T)(object)ULong,
                var t when t == typeof(float)           => (T)(object)Float,
                var t when t == typeof(double)          => (T)(object)Double,
                var t when t == typeof(decimal)         => (T)(object)Decimal,
                var t when t == typeof(bool)            => (T)(object)Bool,
                var t when t == typeof(string)          => (T)(object)String,
                _ => throw new NotSupportedException($"Type '{typeof(T).Name}' is not supported for conversion.")
            };
        }
        
        public override string ToString() => String;

        public bool Equals(CommandArgument other)
        {
            return _value.Span.SequenceEqual(other._value.Span);
        }

        public override bool Equals(object obj)
        {
            return obj is CommandArgument other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in _value.Span)
                {
                    hash = (hash * 31) + c;
                }

                return hash;
            }
        }

        public static bool operator ==(CommandArgument left, CommandArgument right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CommandArgument left, CommandArgument right)
        {
            return !left.Equals(right);
        }
    }
}

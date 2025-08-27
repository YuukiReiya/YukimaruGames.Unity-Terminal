using System;

namespace YukimaruGames.Terminal.Domain.Model
{
    /// <summary>
    /// コマンドの引数型.
    /// </summary>
    public readonly struct CommandArgument : IEquatable<CommandArgument>
    {
        #region Lazy
        private readonly Lazy<sbyte> _sbyteLazy;
        private readonly Lazy<byte> _byteLazy;
        private readonly Lazy<short> _shortLazy;
        private readonly Lazy<ushort> _ushortLazy;
        private readonly Lazy<int> _intLazy;
        private readonly Lazy<uint> _uintLazy;
        private readonly Lazy<long> _longLazy;
        private readonly Lazy<ulong> _ulongLazy;
        private readonly Lazy<float> _floatLazy;
        private readonly Lazy<double> _doubleLazy;
        private readonly Lazy<decimal> _decimalLazy;
        private readonly Lazy<bool> _boolLazy;
        #endregion

        public string String { get; }

        #region 整数型

        /// <summary>
        /// 符号付き8ビット
        /// </summary>
        /// <remarks>
        /// -128 ～ 127
        /// </remarks>
        public sbyte SByte => _sbyteLazy.Value;

        /// <summary>
        /// 符号なし8ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 255
        /// </remarks>
        public byte Byte => _byteLazy.Value;

        /// <summary>
        /// 符号付き16ビット
        /// </summary>
        /// <remarks>
        /// -32,768 ～ 32,767
        /// </remarks>
        public short Short => _shortLazy.Value;

        /// <summary>
        /// 符号なし16ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 65,535
        /// </remarks>
        public ushort UShort => _ushortLazy.Value;

        /// <summary>
        /// 符号付き32ビット
        /// </summary>
        /// <remarks>
        /// -2,147,483,648 ～ 2,147,483,647
        /// </remarks>
        public int Int => _intLazy.Value;

        /// <summary>
        /// 符号なし32ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 4,294,967,295
        /// </remarks>
        public uint UInt => _uintLazy.Value;

        /// <summary>
        /// 符号付き64ビット
        /// </summary>
        /// <remarks>
        /// -9,223,372,036,854,775,808 ～ 9,223,372,036,854,775,807
        /// </remarks>
        public long Long => _longLazy.Value;

        /// <summary>
        /// 符号なし64ビット
        /// </summary>
        /// <remarks>
        /// 0 ～ 18,446,744,073,709,551,615
        /// </remarks>
        public ulong ULong => _ulongLazy.Value;

        #endregion

        #region 浮動小数点型

        /// <summary>
        /// 4バイト
        /// </summary>
        /// <remarks>
        ///	有効桁数　: ~6 ～9 桁
        /// ±1.5 x 10−45 から ±3.4 x 1038
        /// </remarks>
        public float Float => _floatLazy.Value;

        /// <summary>
        /// 8バイト
        /// </summary>
        /// <remarks>
        /// 有効桁数　:　~15-17 桁
        /// ±5.0 × 10−324 - ±1.7 × 10308
        /// </remarks>
        public double Double => _doubleLazy.Value;

        /// <summary>
        /// 16バイト
        /// </summary>
        /// <remarks>
        /// 有効桁数　:　28 から 29 桁の数字
        /// ±1.0 x 10-28 から ±7.9228 x 1028
        /// </remarks>>
        public decimal Decimal => _decimalLazy.Value;

        #endregion

        /// <summary>
        /// bool型への変換
        /// </summary>
        /// <remarks>
        /// 大文字/小文字の判定は無視.
        /// </remarks>
        public bool Bool => _boolLazy.Value;

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="argument">引数</param>
        public CommandArgument(string argument)
        {
            String = argument;
            _sbyteLazy = new Lazy<sbyte>(() => sbyte.Parse(argument));
            _byteLazy = new Lazy<byte>(() => byte.Parse(argument));
            _shortLazy = new Lazy<short>(() => short.Parse(argument));
            _ushortLazy = new Lazy<ushort>(() => ushort.Parse(argument));
            _intLazy = new Lazy<int>(() => int.Parse(argument));
            _uintLazy = new Lazy<uint>(() => uint.Parse(argument));
            _longLazy = new Lazy<long>(() => long.Parse(argument));
            _ulongLazy = new Lazy<ulong>(() => ulong.Parse(argument));
            _floatLazy = new Lazy<float>(() => float.Parse(argument));
            _doubleLazy = new Lazy<double>(() => double.Parse(argument));
            _decimalLazy = new Lazy<decimal>(() => decimal.Parse(argument));
            _boolLazy = new Lazy<bool>(() => bool.Parse(argument));
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
            return String == other.String;
        }

        public override bool Equals(object obj)
        {
            return obj is CommandArgument other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (String != null ? String.GetHashCode() : 0);
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

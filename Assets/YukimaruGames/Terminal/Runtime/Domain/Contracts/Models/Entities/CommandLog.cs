#if !UNITY_2021_2_OR_NEWER
#define FALLBACK
#endif

using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.SharedKernel;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace YukimaruGames.Terminal.Domain.Contracts.Models.Entities
{
    /// <summary>
    /// コマンド実行ログエンティティ.
    /// </summary>
    /// <remarks>
    /// <para><b>サイズ分析 (x64)</b></para>
    /// <para>
    /// フィールド合計: 36 bytes（値型部分）<br/>
    ///   int Id                        =  4 bytes<br/>
    ///   MessageType (byte)            =  1 byte<br/>
    ///   padding                       =  3 bytes<br/>
    ///   DateTimeOffset                = 12 bytes（ticks 8 + offset 4）<br/>
    ///   string Message ref   =  8 bytes（参照ポインタ / heap実体: 16+2N bytes）<br/>
    /// <br/>
    /// string を直接保持（char[] コピーとコスト同等、リテラル利用で string に軍配が上がることを踏まえ string型を採用）<br/>
    /// <br/>
    /// 判定: sealed class（36 bytes &gt; 16 bytes 基準）
    /// </para>
    /// </remarks>
    public
#if FALLBACK
        class CommandLog : IEquatable<CommandLog>, IComparable<CommandLog>, IComparable
#else
        record CommandLog(int Id, MessageType MessageType, DateTimeOffset Timestamp, string Message)
        : IComparable<CommandLog>, IComparable
#endif
    {
        #region FALLBACK
#if FALLBACK
        public CommandLog(int id, MessageType type, DateTimeOffset timestamp, string message)
        {
            Id = id;
            MessageType = type;
            Timestamp = timestamp;
            Message = message;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is CommandLog other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(MessageType, Timestamp, Message);
        public static bool operator ==(CommandLog left, CommandLog right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(CommandLog left, CommandLog right) => !(left == right);

        public bool Equals(CommandLog other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return MessageType == other.MessageType && Timestamp.Equals(other.Timestamp) && Message == other.Message;
        }
#endif
        #endregion
        
        /// <summary>
        /// 一意性を指し示すID.
        /// </summary>
        /// <remarks>size: 4 bytes (int)</remarks>
        public int Id
        {
            get;
#if FALLBACK
            private set;
#endif
        }
#if !FALLBACK
            = Id;
#endif
        
        /// <summary>
        /// ログ出力タイプ.
        /// </summary>
        /// <remarks>size: 1 byte (byte enum) + padding 3 bytes</remarks>
        public MessageType MessageType
        {
            get;
#if FALLBACK
            private set;
#endif
        }
#if !FALLBACK
            = MessageType;   
#endif

        /// <summary>
        /// タイムスタンプ.
        /// </summary>
        /// <remarks>size: 12 bytes (ticks 8 + offset 4)</remarks>
        public DateTimeOffset Timestamp
        {
            get;
#if FALLBACK
            private set;
#endif
        }
#if !FALLBACK
            = Timestamp;
#endif

        /// <summary>
        /// 出力文字列.
        /// </summary>
        /// <remarks>
        /// size: 8 bytes（参照ポインタ / heap実体: 16+2N bytes）<br/>
        /// string を直接保持（char[] コピーとコスト同等、リテラル利用で string に軍配が上がることを踏まえ string型を採用）<br/>
        /// </remarks>
        public string Message
        {
            get;
#if FALLBACK
            private set;
#endif
        }
#if !FALLBACK
            = Message;
#endif

    public int CompareTo(CommandLog other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return ReferenceEquals(null, other) ? 1 : Timestamp.CompareTo(other.Timestamp);
    }

    public int CompareTo(object obj) => obj is CommandLog other ? CompareTo(other) : throw new ArgumentException($"Object is not a {nameof(CommandLog)}", nameof(obj));

    public static bool operator <(CommandLog left, CommandLog right) => Comparer<CommandLog>.Default.Compare(left, right) < 0;
    public static bool operator >(CommandLog left, CommandLog right) => Comparer<CommandLog>.Default.Compare(left, right) > 0;
    public static bool operator <=(CommandLog left, CommandLog right) => Comparer<CommandLog>.Default.Compare(left, right) <= 0;
    public static bool operator >=(CommandLog left, CommandLog right) => Comparer<CommandLog>.Default.Compare(left, right) >= 0;
    }
}

#if !UNITY_2021_2_OR_NEWER
#define FALLBACK
#endif

using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.SharedKernel;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace YukimaruGames.Terminal.Domain.Model
{
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

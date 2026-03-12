#if !UNITY_2021_2_OR_NEWER
#define FALLBACK
#endif

using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.SharedKernel;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace YukimaruGames.Terminal.Application.Dto
{
    public
#if FALLBACK
        class LogEntry : IEquatable<LogEntry>, IComparable<LogEntry>, IComparable
#else
        record LogEntry(int Id, MessageType MessageType, DateTimeOffset Timestamp, string Message)
        : IComparable<LogEntry>, IComparable
#endif
    {
        #region FALLBACK

#if FALLBACK
        public LogEntry(int id, MessageType type, DateTimeOffset timestamp, string message)
        {
            Id = id;
            MessageType = type;
            Timestamp = timestamp;
            Message = message;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is LogEntry other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, MessageType, Timestamp, Message);
        public static bool operator ==(LogEntry left, LogEntry right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(LogEntry left, LogEntry right) => !(left == right);

        public bool Equals(LogEntry other)
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

        public int CompareTo(LogEntry other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            
            var timestampComparison = Timestamp.CompareTo(other.Timestamp);
            return timestampComparison == 0 ? Id.CompareTo(other.Id) : timestampComparison;
        }

        public int CompareTo(object obj) => obj is CommandLog other ? CompareTo(other) : throw new ArgumentException($"Object is not a {nameof(CommandLog)}", nameof(obj));

        public static bool operator <(LogEntry left, LogEntry right) => Comparer<LogEntry>.Default.Compare(left, right) < 0;
        public static bool operator >(LogEntry left, LogEntry right) => Comparer<LogEntry>.Default.Compare(left, right) > 0;
        public static bool operator <=(LogEntry left, LogEntry right) => Comparer<LogEntry>.Default.Compare(left, right) <= 0;
        public static bool operator >=(LogEntry left, LogEntry right) => Comparer<LogEntry>.Default.Compare(left, right) >= 0;
    }
}


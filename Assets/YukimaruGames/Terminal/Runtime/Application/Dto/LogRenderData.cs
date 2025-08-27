#if !UNITY_2021_2_OR_NEWER
#define FALLBACK
#endif

using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.SharedKernel;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace YukimaruGames.Terminal.Application.Model
{
    public
#if FALLBACK
        class LogRenderData : IEquatable<LogRenderData>, IComparable<LogRenderData>, IComparable
#else
        record LogRenderData(int Id, MessageType MessageType, DateTimeOffset Timestamp, string Message)
        : IComparable<LogRenderData>, IComparable
#endif
    {
        #region FALLBACK

#if FALLBACK
        public LogRenderData(int id, MessageType type, DateTimeOffset timestamp, string message)
        {
            Id = id;
            MessageType = type;
            Timestamp = timestamp;
            Message = message;
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is LogRenderData other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, MessageType, Timestamp, Message);
        public static bool operator ==(LogRenderData left, LogRenderData right) => left?.Equals(right) ?? ReferenceEquals(right, null);
        public static bool operator !=(LogRenderData left, LogRenderData right) => !(left == right);

        public bool Equals(LogRenderData other)
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

        public int CompareTo(LogRenderData other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            
            var timestampComparison = Timestamp.CompareTo(other.Timestamp);
            return timestampComparison == 0 ? Id.CompareTo(other.Id) : timestampComparison;
        }

        public int CompareTo(object obj) => obj is CommandLog other ? CompareTo(other) : throw new ArgumentException($"Object is not a {nameof(CommandLog)}", nameof(obj));

        public static bool operator <(LogRenderData left, LogRenderData right) => Comparer<LogRenderData>.Default.Compare(left, right) < 0;
        public static bool operator >(LogRenderData left, LogRenderData right) => Comparer<LogRenderData>.Default.Compare(left, right) > 0;
        public static bool operator <=(LogRenderData left, LogRenderData right) => Comparer<LogRenderData>.Default.Compare(left, right) <= 0;
        public static bool operator >=(LogRenderData left, LogRenderData right) => Comparer<LogRenderData>.Default.Compare(left, right) >= 0;
    }
}


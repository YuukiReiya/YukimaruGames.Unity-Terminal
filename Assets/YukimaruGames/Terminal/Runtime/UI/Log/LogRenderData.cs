#if !UNITY_2021_2_OR_NEWER
#define FALLBACK
#endif
#if FALLBACK
using System;
#endif
using System.Collections.Generic;
using YukimaruGames.Terminal.Application.Commands;

namespace YukimaruGames.Terminal.UI.Log
{
    public
#if FALLBACK
        class LogRenderData : IEquatable<LogRenderData>
#else
        record LogRenderData(IReadOnlyCollection<LogEntry> LogRenderDataCollection)
#endif
    {
        #region FALLBACK

#if FALLBACK
        public LogRenderData(IReadOnlyCollection<LogEntry> logRenderDataCollection)
        {
            LogRenderDataCollection = logRenderDataCollection;
        }

        public bool Equals(LogRenderData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(LogRenderDataCollection, other.LogRenderDataCollection);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is LogRenderData other && Equals(other);

        public override int GetHashCode() => (LogRenderDataCollection != null ? LogRenderDataCollection.GetHashCode() : 0);
#endif

        #endregion

        public IReadOnlyCollection<LogEntry> LogRenderDataCollection { get; }
#if !FALLBACK
            = LogRenderDataCollection;
#endif
    }
}

#if !UNITY_2021_2_OR_NEWER
#define FALLBACK
#endif
#if FALLBACK
using System;
#endif
using System.Collections.Generic;
using YukimaruGames.Terminal.Application.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public
#if FALLBACK
        class TerminalLogRenderData : IEquatable<TerminalLogRenderData>
#else
        record TerminalLogRenderData(IReadOnlyCollection<LogRenderData> LogRenderDataCollection)
#endif
    {
        #region FALLBACK

#if FALLBACK
        public TerminalLogRenderData(IReadOnlyCollection<LogRenderData> logRenderDataCollection)
        {
            LogRenderDataCollection = logRenderDataCollection;
        }

        public bool Equals(TerminalLogRenderData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(LogRenderDataCollection, other.LogRenderDataCollection);
        }

        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is TerminalLogRenderData other && Equals(other);

        public override int GetHashCode() => (LogRenderDataCollection != null ? LogRenderDataCollection.GetHashCode() : 0);
#endif

        #endregion

        public IReadOnlyCollection<LogRenderData> LogRenderDataCollection { get; }
#if !FALLBACK
            = LogRenderDataCollection;
#endif
    }
}

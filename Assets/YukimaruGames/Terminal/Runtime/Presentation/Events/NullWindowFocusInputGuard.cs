using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;

namespace YukimaruGames.Terminal.Presentation.Events
{
    /// <summary>
    /// 何も行わない<see cref="IWindowFocusInputGuard"/>実装.
    /// Legacy Input Manager以外の経路や、対策が不要/無効化されている場合のフォールバックに使う.
    /// </summary>
    public sealed class NullWindowFocusInputGuard : IWindowFocusInputGuard
    {
        private static readonly Lazy<NullWindowFocusInputGuard> LazyInstance = new(() => new NullWindowFocusInputGuard());
        public static NullWindowFocusInputGuard Instance => LazyInstance.Value;

        private sealed class NoopDisposable : IDisposable
        {
            private static readonly Lazy<NoopDisposable> LazyInstance = new(() => new NoopDisposable());
            public static NoopDisposable Instance => LazyInstance.Value;
            void IDisposable.Dispose() { }
        }

        private NullWindowFocusInputGuard() { }

        public IDisposable BeginScope() => NoopDisposable.Instance;
    }
}

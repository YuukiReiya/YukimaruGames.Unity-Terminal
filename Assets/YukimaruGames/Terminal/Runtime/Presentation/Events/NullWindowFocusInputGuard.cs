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
        private static readonly Lazy<NullWindowFocusInputGuard> _lazyInstance = new(() => new NullWindowFocusInputGuard());

        /// <summary>
        /// 唯一の共有インスタンス.
        /// </summary>
        public static NullWindowFocusInputGuard Instance => _lazyInstance.Value;

        private sealed class NoopDisposable : IDisposable
        {
            private static readonly Lazy<NoopDisposable> _lazyInstance = new(() => new NoopDisposable());
            public static NoopDisposable Instance => _lazyInstance.Value;
            void IDisposable.Dispose() { }
        }

        private NullWindowFocusInputGuard() { }

        /// <inheritdoc/>
        public IDisposable BeginScope() => NoopDisposable.Instance;
    }
}

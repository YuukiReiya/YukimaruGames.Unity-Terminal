using System;
using System.Collections.Generic;
using System.Linq;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 構築済みコンポーネントから<see cref="TerminalRuntimeScope"/>を組み立てる.
    /// </summary>
    /// <remarks>
    /// 更新対象(<see cref="IUpdatable"/>)・破棄対象(<see cref="IDisposable"/> /
    /// <see cref="IAsyncDisposable"/>)の振り分けはバックエンドに依存しないため、
    /// 継承ではなく静的ヘルパーとして切り出してある(#145).
    /// </remarks>
    internal static class ScopeBuilder
    {
        /// <summary>
        /// Domain層とバックエンドのコンポーネントを束ねてScopeを構築する.
        /// </summary>
        internal static TerminalRuntimeScope Build(in DomainContext domain, in BackendContext backend)
        {
            var instances = domain.Components.Concat(backend.Components).ToArray();

            var updatables = instances.OfType<IUpdatable>().ToList();
            var asyncDisposables = instances.OfType<IAsyncDisposable>().ToList();
            var disposables = instances.OfType<IDisposable>().Where(d => d is not IAsyncDisposable).ToList();

            var entryPoint = new TerminalEntryPoint(updatables, backend.GUI);

            return new TerminalRuntimeScope(
                entryPoint,
                domain.Service,
                domain.Registry,
                domain.Autocomplete,
                backend.View,
                disposables,
                asyncDisposables,
                domain.Logger);
        }

        /// <summary>
        /// 構築に失敗した際、その時点までに生成済みのコンポーネントを破棄する.
        /// </summary>
        internal static void CleanUp(IReadOnlyList<object> components)
        {
            if (components == null)
            {
                return;
            }

            // Interface 越しの foreach による GC Alloc を避けるため、for で列挙
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] is IDisposable component)
                {
                    component.Dispose();
                }
            }
        }
    }
}

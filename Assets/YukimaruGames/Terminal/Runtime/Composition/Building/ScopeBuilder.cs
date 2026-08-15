using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 構築済みコンポーネントから<see cref="TerminalRuntimeScope"/>を組み立てる.
    /// </summary>
    /// <remarks>
    /// 初期化対象(<see cref="IStartable"/>)・更新対象(<see cref="IUpdatable"/>)・
    /// 破棄対象(<see cref="IDisposable"/> / <see cref="IAsyncDisposable"/>)の振り分けは
    /// バックエンドに依存しないため、継承ではなく静的ヘルパーとして切り出してある(#145).
    /// </remarks>
    internal static class ScopeBuilder
    {
        /// <summary>
        /// Domain層とバックエンドのコンポーネントを束ねてScopeを構築する.
        /// </summary>
        internal static TerminalRuntimeScope Build(in DomainContext domain, in BackendContext backend)
        {
            var instances = domain.Components.Concat(backend.Components).ToArray();

            var startables = instances.OfType<IStartable>().ToList();
            var updatables = instances.OfType<IUpdatable>().ToList();
            var asyncDisposables = instances.OfType<IAsyncDisposable>().ToList();
            var disposables = instances.OfType<IDisposable>().Where(d => d is not IAsyncDisposable).ToList();

            var entryPoint = new TerminalEntryPoint(startables, updatables, backend.GUI);

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
        /// <remarks>
        /// 個々のDisposeで発生した例外はログに留め、送出しない。ここは構築失敗時の後始末経路であり、
        /// 途中で例外を投げると(1)残りのコンポーネントが破棄されず、(2)呼び出し元の<c>throw;</c>へ
        /// 到達できず構築失敗の元例外が失われて診断できなくなるため。
        /// <see cref="TerminalRuntimeScope"/>の同期破棄と同じ方針.
        /// </remarks>
        internal static void CleanUp(IReadOnlyList<object> components)
        {
            if (components == null)
            {
                return;
            }

            // Interface 越しの foreach による GC Alloc を避けるため、for で列挙
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] is not IDisposable component)
                {
                    continue;
                }

                try
                {
                    component.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[YukimaruGames.Terminal] Failed to dispose '{component.GetType().FullName}' while cleaning up a failed installation: {e}");
                }
            }
        }
    }
}

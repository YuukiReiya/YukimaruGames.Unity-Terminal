using System;
using System.Collections.Generic;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// static コマンドへ注入可能な、起動時に確定済みのサービス群.
    /// </summary>
    /// <remarks>
    /// <p>
    /// <see cref="YukimaruGames.Terminal.Infrastructure.Factories.CommandFactory"/> が Expression Tree 生成時に
    /// <c>Expression.Constant</c> として式木へ焼き込むための値を保持する。
    /// </p>
    /// <p>
    /// 型完全一致でのみ解決する(派生・実装の探索はしない)。どのサービス型を保持するかは
    /// Infrastructure 層の関与しないところ(Composition層)で決定される。
    /// </p>
    /// </remarks>
    public readonly struct ModeServiceBundle
    {
        /// <summary>
        /// 何も注入しないバンドル.
        /// </summary>
        public static readonly ModeServiceBundle Empty = default;

        private readonly IReadOnlyDictionary<Type, object> _services;

        public ModeServiceBundle(IReadOnlyDictionary<Type, object> services)
        {
            _services = services;
        }

        /// <summary>
        /// 指定した型に対応するサービスの解決を試みる.
        /// </summary>
        /// <param name="parameterType">解決したいパラメータの型</param>
        /// <param name="service">解決されたサービスインスタンス</param>
        /// <returns>解決できた場合は true.</returns>
        public bool TryResolve(Type parameterType, out object service)
        {
            if (_services != null)
            {
                return _services.TryGetValue(parameterType, out service);
            }

            service = null;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;

namespace YukimaruGames.Terminal.Domain.Contracts.Modes
{
    /// <summary>
    /// static コマンドへ注入可能な、起動時に確定済みのサービス群.
    /// </summary>
    /// <remarks>
    /// <p>
    /// <c>CommandFactory</c> が Expression Tree 生成時に
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

        /// <summary>
        /// 型ごとの注入対象サービスを指定して初期化する.
        /// </summary>
        /// <param name="services">型からサービスインスタンスへのマップ(<c>null</c>可、その場合は常に解決失敗する).</param>
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

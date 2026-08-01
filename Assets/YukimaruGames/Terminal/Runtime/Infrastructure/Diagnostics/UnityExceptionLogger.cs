using System;
using UnityEngine;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Providers;

namespace YukimaruGames.Terminal.Infrastructure.Diagnostics
{
    /// <summary>
    /// <see cref="Debug.LogException(Exception)"/>を介して例外を記録する.
    /// </summary>
    public sealed class UnityExceptionLogger : IExceptionLogger
    {
        /// <summary>例外を記録する.</summary>
        /// <param name="exception">記録対象の例外.</param>
        public void Log(Exception exception) => Debug.LogException(exception);
    }
}

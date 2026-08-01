using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Contracts;

namespace YukimaruGames.Terminal.Infrastructure.Diagnostics
{
    public sealed class UnityExceptionLogger : IExceptionLogger
    {
        public void Log(Exception exception) => Debug.LogException(exception);
    }
}

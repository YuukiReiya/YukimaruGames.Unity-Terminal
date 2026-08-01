using System;

namespace YukimaruGames.Terminal.Presentation.Contracts
{
    /// <summary>
    /// 例外の記録先.
    /// <para>
    /// Presentation層はUnityEngine.Debugに直接依存しないため、この抽象を介して例外を記録する。
    /// </para>
    /// </summary>
    public interface IExceptionLogger
    {
        /// <summary>例外を記録する.</summary>
        void Log(Exception exception);
    }
}

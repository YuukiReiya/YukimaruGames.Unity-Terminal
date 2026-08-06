using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Application.Services.Modes
{
    /// <summary>
    /// <see cref="IModeContext"/> の実装.
    /// </summary>
    internal sealed class ModeContext : IModeContext
    {
        /// <inheritdoc/>
        public ICommandRegistry Commands { get; }

        /// <inheritdoc/>
        public IModeOutput Output { get; }

        /// <inheritdoc/>
        public IModeTransitionRequestSink Transitions { get; }

        /// <inheritdoc/>
        public IModeStackInspector Stack { get; }

        /// <summary>
        /// 各窓口を指定して初期化する.
        /// </summary>
        public ModeContext(ICommandRegistry commands, IModeOutput output, IModeTransitionRequestSink transitions, IModeStackInspector stack)
        {
            Commands = commands;
            Output = output;
            Transitions = transitions;
            Stack = stack;
        }
    }
}

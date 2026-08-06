using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Application.Services.Modes
{
    /// <summary>
    /// <see cref="IModeContext"/> の実装.
    /// </summary>
    internal sealed class ModeContext : IModeContext
    {
        public ICommandRegistry Commands { get; }
        public IModeOutput Output { get; }
        public IModeTransitionRequestSink Transitions { get; }
        public IModeStackInspector Stack { get; }

        public ModeContext(ICommandRegistry commands, IModeOutput output, IModeTransitionRequestSink transitions, IModeStackInspector stack)
        {
            Commands = commands;
            Output = output;
            Transitions = transitions;
            Stack = stack;
        }
    }
}

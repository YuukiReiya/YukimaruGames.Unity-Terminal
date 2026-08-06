using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Application.Services.Modes
{
    /// <summary>
    /// <see cref="ICommandLogger"/> をラップする <see cref="IModeOutput"/> 実装.
    /// </summary>
    internal sealed class LoggerModeOutput : IModeOutput
    {
        private readonly ICommandLogger _logger;

        public LoggerModeOutput(ICommandLogger logger)
        {
            _logger = logger;
        }

        void IModeOutput.Message(string message) => _logger?.Send(MessageType.Message, message);

        void IModeOutput.Warning(string message) => _logger?.Send(MessageType.Warning, message);

        void IModeOutput.Error(string message) => _logger?.Send(MessageType.Error, message);
    }
}

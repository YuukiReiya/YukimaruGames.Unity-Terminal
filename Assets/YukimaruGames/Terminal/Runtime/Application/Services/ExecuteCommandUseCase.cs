using System;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Domain.Abstractions.Exceptions;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Application.Services
{
    /// <summary>
    /// コマンド実行ユースケース.
    /// </summary>
    public sealed class ExecuteCommandUseCase : IExecuteCommandUseCase
    {
        private readonly ICommandLogger _logger;
        private readonly ICommandRegistry _registry;
        private readonly ICommandInvoker _invoker;
        private readonly ICommandParser _parser;
        private readonly ICommandHistory _history;

        public ExecuteCommandUseCase(
            ICommandLogger logger,
            ICommandRegistry registry,
            ICommandInvoker invoker,
            ICommandParser parser,
            ICommandHistory history)
        {
            _logger = logger;
            _registry = registry;
            _invoker = invoker;
            _parser = parser;
            _history = history;
        }

        /// <inheritdoc/>
        public ValueTask ExecuteAsync(string str) => ExecuteAsync(str.AsMemory());

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(ReadOnlyMemory<char> str)
        {
            // history は string API のため ToString() で変換（Adapters境界）
            var input = str.ToString();
            _logger?.Send(MessageType.Entry, input);
            _history.Add(input);

            var result = await _parser.ParseAsync(str);
            if (string.IsNullOrEmpty(result.Command))
            {
                return;
            }

            if (!_registry.TryGet(result.Command, out var handler))
            {
                _logger?.Send(MessageType.Error, $"No such command: '{result.Command}'.");
                return;
            }

            if (0 < (result.Status & ICommandParser.ParseStatusCode.SyntaxError))
            {
                _logger?.Send(
                    MessageType.Error,
                    $"Invalid string format: \"{input}\" is not enclosed with single (\') or double (\") quotes.");
                return;
            }

            try
            {
                var arguments = result.Arguments?.AsMemory() ?? ReadOnlyMemory<CommandArgument>.Empty;
                _invoker.Execute(handler, arguments);
            }
            catch (CommandArgumentException e)
            {
                _logger?.Send(MessageType.Exception, $"Error: {e.Message}");
            }
            catch (CommandFormatException e)
            {
                _logger?.Send(MessageType.Exception, $"Error: {e.Message}");
            }
            catch (Exception e)
            {
                _logger?.Send(MessageType.Exception, $"{e.GetType().Name}: {e.Message}");
            }
        }
    }
}

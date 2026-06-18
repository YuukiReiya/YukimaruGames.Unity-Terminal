using System;
using System.Threading;
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
        void IExecuteCommandUseCase.Execute(string str) => ((IExecuteCommandUseCase)this).Execute(str.AsMemory());
        
        /// <inheritdoc/>
        void IExecuteCommandUseCase.Execute(ReadOnlyMemory<char> str)
        {
            if (!TryPrepareExecute(str, default, out var command, out var handler, out var arguments))
            {
                return;
            }

            try
            {
                _invoker.Execute(handler, arguments);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        /// <inheritdoc/>
        ValueTask IExecuteCommandUseCase.ExecuteAsync(string str, CancellationToken cancellationToken) => ((IExecuteCommandUseCase)this).ExecuteAsync(str.AsMemory(), cancellationToken);

        /// <inheritdoc/>
        async ValueTask IExecuteCommandUseCase.ExecuteAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken)
        {
            if (!TryPrepareExecute(str, cancellationToken, out var command, out var handler, out var arguments))
            {
                return;
            }

            try
            {
                // TODO: 非同期メソッドへの対応
                _invoker.Execute(handler, arguments);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }
        
        
        /// <summary>
        /// 【共通前処理】パース、ログ記録、バリデーションをまとめて行います（アロケーションフリー）。
        /// </summary>
        private bool TryPrepareExecute(
            ReadOnlyMemory<char> str, 
            CancellationToken cancellationToken,
            out string command,
            out CommandHandler handler, // ★プロジェクトの実際のハンドラー型に変えてください
            out ReadOnlyMemory<CommandArgument> arguments)
        {
            command = default;
            handler = default;
            arguments = default;

            // 事前キャンセルチェック
            if (cancellationToken.IsCancellationRequested) return false;

            // パース実行（高速な同期処理）
            var resultCode = _parser.Parse(str, out var tuple);
            command = tuple.Command;

            if (string.IsNullOrEmpty(command))
            {
                return false;
            }

            // ログと履歴への追加
            var input = str.ToString();
            _logger?.Send(MessageType.Entry, input);
            _history?.Add(input);

            // レジストリからハンドラーの取得チェック
            if (!_registry.TryGet(command, out handler))
            {
                _logger?.Send(MessageType.Error, $"No such command: '{command}'.");
                return false;
            }

            // 構文エラーチェック
            if (0 < (resultCode & ICommandParser.ParseStatusCode.SyntaxError))
            {
                _logger?.Send(
                    MessageType.Error,
                    $"Invalid string format: \"{input}\" is not enclosed with single (\') or double (\") quotes.");
                return false;
            }

            // 引数の確定
            arguments = tuple.Arguments?.AsMemory() ?? ReadOnlyMemory<CommandArgument>.Empty;
            return true;
        }
        
        /// <summary>
        /// 【共通後処理】発生した例外のログ出力を一括ハンドリングします。
        /// </summary>
        private void HandleException(Exception e)
        {
            if (e is CommandArgumentException or CommandFormatException)
            {
                _logger?.Send(MessageType.Exception, $"Error: {e.Message}");
            }
            else
            {
                _logger?.Send(MessageType.Exception, $"{e.GetType().Name}: {e.Message}");
            }
        }
    }
}
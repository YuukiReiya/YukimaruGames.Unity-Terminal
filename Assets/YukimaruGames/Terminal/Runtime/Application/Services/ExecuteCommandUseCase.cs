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

        private CancellationTokenSource _currentCommandCts;

        /// <summary>
        /// 非ロック(コマンド実行可能)状態
        /// </summary>
        /// <remarks>
        /// 原子性・レースコンディションの回避用
        /// </remarks>
        private const int Idle = 0;
        
        /// <summary>
        /// ロック(コマンド実行不可)状態
        /// </summary>
        /// <remarks>
        /// 原子性・レースコンディションの回避用
        /// </remarks>
        private const int Executing = 1;
        
        /// <remarks>
        /// ロック用のフラグ
        /// <p>0 : Idle</p>
        /// <p>1 : Executing</p>
        /// </remarks> 
        private int _isExecutingState = Idle;

        /// <inheritdoc/>
        /// <remarks>
        /// 値を変えずに最新の状態を確認(キャッシュ無視)
        /// </remarks>
        public bool IsExecuting => Volatile.Read(ref _isExecutingState) == Executing;
        
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
            if (Interlocked.CompareExchange(ref _isExecutingState, Executing, Idle) == Executing)
            {
                return;
            }
            
            try
            {
                if (!TryPrepareExecute(str, default, out var command, out var handler, out var arguments))
                {
                    return;
                }

                if (handler.IsAsync)
                {
                    _logger?.Send(MessageType.Error, $"The command '{command}' requires asynchronous execution. Please use ExecuteAsync.");
                    return;
                }

                _invoker.Execute(handler, arguments);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
            finally
            {
                Interlocked.Exchange(ref _isExecutingState, Idle);
            }
        }

        /// <inheritdoc/>
        ValueTask IExecuteCommandUseCase.ExecuteAsync(string str, CancellationToken cancellationToken) => ((IExecuteCommandUseCase)this).ExecuteAsync(str.AsMemory(), cancellationToken);

        /// <inheritdoc/>
        void IExecuteCommandUseCase.CancelCommandIfNeeded() => _currentCommandCts?.Cancel();

        /// <inheritdoc/>
        async ValueTask IExecuteCommandUseCase.ExecuteAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken)
        { 
            // 確認 + 書き換え
            if (Interlocked.CompareExchange(ref _isExecutingState, Executing, Idle) == Executing)
            {
                return;
            }

            try
            {
                if (!TryPrepareExecute(str, cancellationToken, out _, out var handler, out var arguments))
                {
                    return;
                }
                
                // 登録プロシージャに応じて同期メソッド非同期メソッドを呼び出しわける
                if (handler.IsAsync)
                {
                    _currentCommandCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    await _invoker.ExecuteAsync(handler, arguments, _currentCommandCts.Token);
                }
                else
                {
                    // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                    _invoker.Execute(handler, arguments);
                }
            }
            catch (Exception e)
            {
                HandleException(e);
            }
            finally
            {
                _currentCommandCts?.Dispose();
                _currentCommandCts = null;

                Interlocked.Exchange(ref _isExecutingState, Idle);
            }
        }
        
        
        /// <summary>
        /// 【共通前処理】パース、ログ記録、バリデーションをまとめて行います（アロケーションフリー）。
        /// </summary>
        private bool TryPrepareExecute(
            ReadOnlyMemory<char> str, 
            CancellationToken cancellationToken,
            out string command,
            out CommandHandler handler,
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
            if (e is OperationCanceledException)
            {
                // キャンセル
                return;
            }

            switch (e)
            {
                // カスタム例外.
                case CommandArgumentException or CommandFormatException:
                    _logger?.Send(MessageType.Exception, $"Error: {e.Message}");    
                    break;
                
                // 一般例外.
                default:
                    _logger?.Send(MessageType.Exception, $"{e.GetType().Name}: {e.Message}");
                    break;
            }
        }
    }
}
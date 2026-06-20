using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Mappers;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Models.Entities;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Application.Services
{
    /// <summary>
    /// ドメインサービスを全般を統括し、外部へAPIを提供するアプリケーションサービス.
    /// </summary>
    /// <remarks>
    /// <p>Facade&lt;窓口&gt;</p>
    /// 上位レイヤーはドメイン層へ直接アクセスするのではなく、本クラスを介してその機能を利用する。
    /// </remarks>
    public sealed class TerminalService : ITerminalService, IDisposable
    {
        private readonly ICommandLogger _logger;
        private readonly ICommandRegistry _registry;
        private readonly ICommandHistory _history;
        private readonly ICommandAutocomplete _autocomplete;
        private readonly IExecuteCommandUseCase _executeCommandUseCase;

        private Action _onLogUpdated;
        private Action<LogEntry[]> _onLogAdded;
        private Action<LogEntry[]> _onLogRemoved;

        /// <inheritdoc/>
        /// <remarks>
        /// <p>ステートレス</p>
        /// <p>プロパティの呼び出しの度にMapperを介したDtoのマッピングが行われるため呼び出し側でキャッシュする機構が望まれる</p>
        /// </remarks>
        public IReadOnlyCollection<LogEntry> Logs => 0 < (_logger?.Logs?.Count ?? 0) ? LogMapper.Mapping(_logger.Logs.ToArray()) : Array.Empty<LogEntry>();
        
        /// <inheritdoc/>
        /// <remarks>
        /// <p>Queueを利用した</p>
        /// <p>削除・追加が同時に行われても変わらず一度だけの呼び出し.</p>
        /// </remarks>
        public event Action OnLogUpdated
        {
            add => _onLogUpdated += value;
            remove => _onLogUpdated -= value;
        }

        /// <inheritdoc/>
        public event Action<LogEntry[]> OnLogAdded
        {
            add => _onLogAdded += value;
            remove => _onLogAdded -= value;
        }

        /// <inheritdoc/>
        public event Action<LogEntry[]> OnLogRemoved
        {
            add => _onLogRemoved += value;
            remove => _onLogRemoved -= value;
        }
        
        public TerminalService(
            ICommandLogger logger,
            ICommandRegistry registry,
            ICommandHistory history,
            ICommandAutocomplete autocomplete,
            IExecuteCommandUseCase executeCommandUseCase)
        {
            _logger = logger;
            _registry = registry;
            _history = history;
            _autocomplete = autocomplete;
            _executeCommandUseCase = executeCommandUseCase;

            if (_logger != null)
            {
                _logger.OnItemUpdated += OnLogItemUpdated;
                _logger.OnItemAdded += OnLogItemAdded;
                _logger.OnItemRemoved += OnLogItemRemoved;
            }
        }

        /// <summary>
        /// コマンドの登録.
        /// </summary>
        /// <param name="command">登録コマンド名</param>
        /// <param name="handler">登録ハンドラー</param>
        /// <param name="supportsAutocomplete">コマンドを自動補完の補完先として登録するか</param>
        public bool Register(string command, CommandHandler handler, bool supportsAutocomplete = true)
        {
            // ReSharper disable once InvertIf
            if (_registry.Add(command, handler) && supportsAutocomplete)
            {
                _autocomplete.Register(command);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsExecuting => _executeCommandUseCase.IsExecuting;

        /// <inheritdoc/>
        ValueTask ITerminalService.ExecuteAsync(string str, CancellationToken cancellationToken) => _executeCommandUseCase.ExecuteAsync(str, cancellationToken);

        /// <inheritdoc/>
        public void Cancel() => _executeCommandUseCase.CancelCommandIfNeeded();

        /// <inheritdoc/>
        int ITerminalService.LogBufferSize => _logger?.MaxLogs ?? 0;
        
        /// <inheritdoc/>
        void ITerminalService.ResetLogs() => _logger.Clear();

        /// <inheritdoc/>
        void ITerminalService.Message(string message) => _logger?.Send(MessageType.Message, message);

        /// <inheritdoc/>
        void ITerminalService.Warning(string message) => _logger?.Send(MessageType.Warning, message);

        /// <inheritdoc/>
        void ITerminalService.Error(string message) => _logger?.Send(MessageType.Error, message);

        /// <inheritdoc/>
        void ITerminalService.Assert(string message) => _logger?.Send(MessageType.Assert, message);

        /// <inheritdoc/>
        void ITerminalService.Exception(string message) => _logger?.Send(MessageType.Exception, message);

        /// <inheritdoc/>
        void ITerminalService.InputMessage(string message) => _logger?.Send(MessageType.Entry, message);

        /// <inheritdoc/>
        void ITerminalService.SystemMessage(string message) => _logger?.Send(MessageType.System, message);

        /// <inheritdoc cref="ICommandHistory.Next"/> 
        string ITerminalService.NextHistory() => _history.Next();

        /// <inheritdoc cref="ICommandHistory.Previous"/> 
        string ITerminalService.PrevHistory() => _history.Previous();

        /// <inheritdoc cref="ICommandAutocomplete.Complete"/> 
        string[] ITerminalService.Autocomplete(string partialWord) => _autocomplete.Complete(partialWord);

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            if (_logger != null)
            {
                _logger.OnItemUpdated -= OnLogItemUpdated;
                _logger.OnItemAdded -= OnLogItemAdded;
                _logger.OnItemRemoved -= OnLogItemRemoved;
            }

            _onLogUpdated = null;
            _onLogAdded = null;
            _onLogRemoved = null;
        }

        /// <summary>
        /// ロガーの要素が追加・削除された後の呼び出し.
        /// </summary>
        /// <remarks>
        /// Queueを利用した実装の都合上、削除が行われる際は削除＋追加が発生するが、その際も呼び出しは一度だけ.
        /// </remarks>
        private void OnLogItemUpdated() => _onLogUpdated?.Invoke();
        
        /// <summary>
        /// ロガーの要素が追加された後の呼び出し.
        /// </summary>
        /// <param name="logEntries">追加された要素</param>
        private void OnLogItemAdded(CommandLog[] logEntries)
        {
            var array = LogMapper.Mapping(logEntries);
            _onLogAdded?.Invoke(array);
        }

        /// <summary>
        /// ロガーの要素が削除された後の呼び出し.
        /// </summary>
        /// <param name="logEntries">削除された要素</param>
        private void OnLogItemRemoved(CommandLog[] logEntries)
        {
            var array = LogMapper.Mapping(logEntries);
            _onLogRemoved?.Invoke(array);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using YukimaruGames.Terminal.Application.Mapper;
using YukimaruGames.Terminal.Application.Model;
using YukimaruGames.Terminal.Domain.Exception;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Application
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
        private readonly ICommandInvoker _invoker;
        private readonly ICommandParser _parser;
        private readonly ICommandHistory _history;
        private readonly ICommandAutocomplete _autocomplete;
        private readonly ICommandDiscoverer _discoverer;

        private Action _onLogUpdated;
        private Action<LogRenderData[]> _onLogAdded;
        private Action<LogRenderData[]> _onLogRemoved;

        /// <inheritdoc/>
        /// <remarks>
        /// <p>ステートレス</p>
        /// <p>プロパティの呼び出しの度にMapperを介したDtoのマッピングが行われるため呼び出し側でキャッシュする機構が望まれる</p>
        /// </remarks>
        public IReadOnlyCollection<LogRenderData> Logs => 0 < (_logger?.Logs?.Count ?? 0) ? LogMapper.Mapping(_logger.Logs.ToArray()) : Array.Empty<LogRenderData>();
        
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
        public event Action<LogRenderData[]> OnLogAdded
        {
            add => _onLogAdded += value;
            remove => _onLogAdded -= value;
        }

        /// <inheritdoc/>
        public event Action<LogRenderData[]> OnLogRemoved
        {
            add => _onLogRemoved += value;
            remove => _onLogRemoved -= value;
        }
        
        public TerminalService(
            ICommandLogger logger,
            ICommandRegistry registry,
            ICommandInvoker invoker,
            ICommandParser parser,
            ICommandHistory history,
            ICommandAutocomplete autocomplete)
        {
            _logger = logger;
            _registry = registry;
            _invoker = invoker;
            _parser = parser;
            _history = history;
            _autocomplete = autocomplete;

            if (_logger != null)
            {
                _logger.OnItemUpdated += OnLogItemUpdated;
                _logger.OnItemAdded += OnLogItemAdded;
                _logger.OnItemRemoved += OnLogItemRemoved;
            }
            
            // ロガーに登録されたログを詰め込んでからイベントを登録することで初回登録にかかるイベント呼び出しのオーバヘッドを削減.
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
        void ITerminalService.Execute(string str)
        {
            _logger?.Send(MessageType.Entry, str);
            _history.Add(str);

            var result = _parser.Parse(str, out var parse);

            if (string.IsNullOrEmpty(parse.Command))
            {
                return;
            }

            if (!_registry.TryGet(parse.Command, out var handler))
            {
                _logger?.Send(MessageType.Error, $"No such command: '{parse.Command}'.");
                return;
            }

            // 構文エラー.
            if (0 < (result & ICommandParser.ParseStatusCode.SyntaxError))
            {
                _logger?.Send(
                    MessageType.Error,
                    $"Invalid string format: \"{str}\" is not enclosed with single (\') or double (\") quotes.");
            }

            try
            {
                _invoker.Execute(handler, parse.Arguments);
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
        public void Dispose()
        {
            // TODO マネージリソースをここで解放します
            if (_logger != null)
            {
                _logger.OnItemUpdated -= OnLogItemUpdated;
                _logger.OnItemAdded -= OnLogItemAdded;
                _logger.OnItemRemoved -= OnLogItemRemoved;
            }
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

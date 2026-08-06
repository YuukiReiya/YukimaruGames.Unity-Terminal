using System;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Modes.Null;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Services
{
    /// <summary>
    /// 「通常状態」を1つのモードとして表現する実装.
    /// </summary>
    /// <remarks>
    /// 従来 <c>ExecuteCommandUseCase.TryPrepareExecute</c> が担っていた
    /// パース・履歴登録・エコー・レジストリ解決・実行 のロジックをここに移設する。
    /// モードスタックの最下段に常駐し、Popされることはない.
    /// </remarks>
    public sealed class NormalMode : ITerminalMode
    {
        private readonly ICommandLogger _logger;
        private readonly ICommandRegistry _registry;
        private readonly ICommandInvoker _invoker;
        private readonly ICommandParser _parser;
        private readonly ICommandHistory _history;
        private readonly ICommandAutocomplete _autocomplete;

        public NormalMode(
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
            // ITerminalMode.History/Autocomplete はnullを許容しない契約(既定実装のNull Objectを
            // 返すこと)なので、コンストラクタ時点でNull Objectへ差し替える.
            _history = history ?? NullCommandHistory.Instance;
            _autocomplete = autocomplete ?? NullCommandAutocomplete.Instance;
        }

        /// <inheritdoc/>
        public string Id => "normal";

        /// <inheritdoc/>
        /// <remarks>
        /// <c>ITerminalOptions.Prompt</c> の値を Installer が配線時にここへ設定する
        /// (Push先を <c>PromptRenderer</c> から本プロパティへ変更した).
        /// </remarks>
        public string Prompt { get; set; } = "$";

        /// <inheritdoc/>
        public string ContinuationPrompt => Prompt;

        /// <inheritdoc/>
        public ICommandHistory History => _history;

        /// <inheritdoc/>
        public ICommandAutocomplete Autocomplete => _autocomplete;

        /// <inheritdoc/>
        public bool AllowsConcurrentSpinner => false;

        /// <inheritdoc/>
        public ValueTask OnEnterAsync(IModeContext context, CancellationToken cancellationToken) => default;

        /// <inheritdoc/>
        public ValueTask<ModeResult> HandleAsync(ModeInput input, IModeContext context, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<ModeResult>(ModeResult.Continue);
            }

            if (!TryPrepareExecute(input.Text, out _, out var handler, out var arguments))
            {
                return new ValueTask<ModeResult>(ModeResult.Continue);
            }

            if (handler.IsAsync)
            {
                return ExecuteAsyncCommand(handler, arguments, cancellationToken);
            }

            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            _invoker.Execute(handler, arguments);
            return new ValueTask<ModeResult>(ModeResult.Continue);
        }

        /// <inheritdoc/>
        public InterruptDisposition OnInterrupt(bool isCommandRunning) => InterruptDisposition.NotHandled;

        /// <inheritdoc/>
        public ValueTask OnExitAsync(ModeExitReason reason) => default;

        private async ValueTask<ModeResult> ExecuteAsyncCommand(CommandHandler handler, ReadOnlyMemory<CommandArgument> arguments, CancellationToken cancellationToken)
        {
            await _invoker.ExecuteAsync(handler, arguments, cancellationToken);
            return ModeResult.Continue;
        }

        /// <summary>
        /// 【共通前処理】パース、ログ記録、バリデーションをまとめて行います。
        /// </summary>
        private bool TryPrepareExecute(
            ReadOnlyMemory<char> str,
            // ReSharper disable once OutParameterValueIsAlwaysDiscarded.Local
            out string command,
            out CommandHandler handler,
            out ReadOnlyMemory<CommandArgument> arguments)
        {
            command = default;
            handler = default;
            arguments = default;

            // パース実行(高速な同期処理)
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
    }
}

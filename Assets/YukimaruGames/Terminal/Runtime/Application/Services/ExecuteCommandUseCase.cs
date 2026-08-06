using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Exceptions;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Modes.Null;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Application.Services
{
    /// <summary>
    /// コマンド実行ユースケース(ディスパッチャ).
    /// </summary>
    /// <remarks>
    /// 「通常状態も1つのモード」として統一するモードスタック方式の中核。
    /// モードスタックの唯一の所有者であり、実際の1行解釈は現在モード
    /// (<see cref="ITerminalMode.HandleAsync"/>)へ委譲するだけの薄い層になる。
    /// CTS(<see cref="CancellationTokenSource"/>)の生成・破棄、排他ロック、
    /// モード遷移の適用(パイプライン境界でのみ行う)は全てここに集約する.
    /// </remarks>
    public sealed class ExecuteCommandUseCase : IExecuteCommandUseCase
    {
        /// <summary>
        /// 非ロック(コマンド実行可能)状態
        /// </summary>
        private const int Idle = 0;

        /// <summary>
        /// ロック(コマンド実行不可)状態
        /// </summary>
        private const int Executing = 1;

        /// <summary>
        /// モード遷移の連鎖適用の上限. 無限ループ化を防ぐための保険.
        /// </summary>
        private const int MaxCascadeDepth = 8;

        private readonly ICommandLogger _logger;
        private readonly TerminalModeStack _stack;
        private readonly ModeTransitionRequestSink _sink;
        private readonly LoggerModeOutput _output;
        private readonly IModeCommandBinder _binder;
        private readonly System.Text.StringBuilder _continuationBuffer = new();
        private readonly SemaphoreSlim _transitionGate = new(1, 1);
        private readonly StackInspector _stackInspector;

        private CancellationTokenSource _currentCommandCts;
        private int _isExecutingState = Idle;
        private int _disposedState;

        public ExecuteCommandUseCase(ICommandLogger logger, ITerminalMode root, IModeCommandBinder binder = null)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));

            _logger = logger;
            _output = new LoggerModeOutput(logger);
            _binder = binder ?? NullModeCommandBinder.Instance;
            _sink = new ModeTransitionRequestSink(logger);
            _stackInspector = StackInspector.From(this);

            var rootContext = BuildContextFor(root);
            _stack = new TerminalModeStack(root, rootContext);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 値を変えずに最新の状態を確認(キャッシュ無視)
        /// </remarks>
        public bool IsExecuting => Volatile.Read(ref _isExecutingState) == Executing;

        /// <inheritdoc/>
        public bool IsAwaitingContinuation => _continuationBuffer.Length > 0;

        /// <inheritdoc/>
        public string Prompt => IsAwaitingContinuation ? _stack.Current.ContinuationPrompt : _stack.Current.Prompt;

        /// <inheritdoc/>
        public bool AllowsConcurrentSpinner => _stack.Current.AllowsConcurrentSpinner;

        /// <inheritdoc/>
        public int Depth => _stack.Depth;

        /// <inheritdoc/>
        public IModeOutput Output => _output;

        /// <inheritdoc/>
        public IModeTransitionRequestSink Transitions => _sink;

        /// <inheritdoc/>
        async ValueTask IExecuteCommandUseCase.ExecutePipelineAsync(ReadOnlyMemory<char> str, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _disposedState) != 0)
            {
                return;
            }

            // 確認 + 書き換え
            if (Interlocked.CompareExchange(ref _isExecutingState, Executing, Idle) == Executing)
            {
                return;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var current = _stack.Current;
                var wasContinuation = IsAwaitingContinuation;

                // _continuationBuffer は「確定済みの継続入力」のみを保持する不変条件とし、
                // 連結はローカル変数で行う(バッファへは成功パスでのみ書き込む).
                var accumulatedText = wasContinuation
                    ? string.Concat(_continuationBuffer.ToString(), "\n", str.ToString())
                    : str.ToString();

                var input = new ModeInput(accumulatedText.AsMemory(), wasContinuation);
                var context = _stack.CurrentContext;

                _currentCommandCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var ct = _currentCommandCts.Token;

                var turnId = _sink.BeginTurn(current);
                ModeResult result;
                try
                {
                    result = await current.HandleAsync(input, context, ct);
                }
                catch (OperationCanceledException)
                {
                    // キャンセルは正常系として扱う。モードも変更しない。
                    // 継続入力もbashのCtrl+C準拠で破棄する.
                    _continuationBuffer.Clear();
                    _sink.Abort(turnId);
                    return;
                }
                catch (Exception e)
                {
                    // 例外時はモード変更しない(積まれた遷移要求は破棄=トランザクショナル)。
                    // 継続入力バッファも破棄し、「旧状態+失敗行」という不整合な状態を防ぐ.
                    _continuationBuffer.Clear();
                    _sink.Abort(turnId);
                    HandleException(e);
                    return;
                }

                _continuationBuffer.Clear();
                if (result == ModeResult.NeedMoreInput)
                {
                    _continuationBuffer.Append(accumulatedText);
                }

                var requests = _sink.EndTurn(turnId);
                if (requests.Length > 0)
                {
                    await ApplyTransitionsAsync(requests, ct);
                }
            }
            finally
            {
                // Dispose が呼び出されるまでの一瞬の間に Cancel が呼び出されてもいいように先に null を入れておく.
                var cts = _currentCommandCts;
                _currentCommandCts = null;
                cts?.Dispose();

                Interlocked.Exchange(ref _isExecutingState, Idle);
            }
        }

        /// <inheritdoc/>
        void IExecuteCommandUseCase.Interrupt()
        {
            if (Volatile.Read(ref _disposedState) != 0)
            {
                return;
            }

            if (IsExecuting)
            {
                try
                {
                    _currentCommandCts?.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Dispose後の呼び出しは考慮しなくていいので握りつぶす.
                }

                return;
            }

            // 非実行中(モード入力待ち)の割り込みは非同期のPopを伴いうるため fire-and-forget する.
            // UIスレッドから同期的に呼ばれる Interrupt() 自体をブロックしないため.
            _ = InterruptAsync();
        }

        private async ValueTask InterruptAsync()
        {
            try
            {
                if (Volatile.Read(ref _disposedState) != 0)
                {
                    return;
                }

                if (_stack.Depth <= 1)
                {
                    // NormalModeのみ: 抜ける先が無いので何もしない.
                    return;
                }

                var top = _stack.Current;
                InterruptDisposition disposition;
                try
                {
                    disposition = top.OnInterrupt(isCommandRunning: false);
                }
                catch (Exception e)
                {
                    _logger?.Send(MessageType.Warning, $"OnInterrupt threw ({e.GetType().Name}: {e.Message}); treated as NotHandled.");
                    disposition = InterruptDisposition.NotHandled;
                }

                // 継続入力中の割り込みはバッファを破棄する(bashのCtrl+C準拠).
                _continuationBuffer.Clear();

                if (disposition == InterruptDisposition.Handled)
                {
                    return;
                }

                await _transitionGate.WaitAsync();
                try
                {
                    if (Volatile.Read(ref _disposedState) != 0)
                    {
                        return;
                    }

                    if (!ReferenceEquals(_stack.Current, top))
                    {
                        _logger?.Send(MessageType.Warning, "Interrupt was requested against a mode that is no longer current. Discarded.");
                        return;
                    }

                    await PopOneAsync(ModeExitReason.Interrupted);
                }
                finally
                {
                    _transitionGate.Release();
                }
            }
            catch (Exception e)
            {
                _logger?.Send(MessageType.Exception, $"Interrupt handling failed: {e}");
            }
        }

        /// <inheritdoc/>
        public string NextHistory() => _stack.Current.History.Next();

        /// <inheritdoc/>
        public string PrevHistory() => _stack.Current.History.Previous();

        /// <inheritdoc/>
        public string[] Autocomplete(string partialWord) => _stack.Current.Autocomplete.Complete(partialWord);

        /// <inheritdoc/>
        public IReadOnlyList<ModeStackFrameInfo> Snapshot() => _stack.Snapshot();

        /// <inheritdoc/>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposedState, 1) == 1)
            {
                return;
            }

            try
            {
                _currentCommandCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            // Unity の OnDestroy 等、同期経路からのフォールバック. 完走は保証しない(ログのみ).
            _ = FireAndForgetShutdownAsync();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposedState, 1) == 1)
            {
                return;
            }

            try
            {
                _currentCommandCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            await _transitionGate.WaitAsync();
            try
            {
                await PopAllAsync(ModeExitReason.Shutdown);
            }
            finally
            {
                _transitionGate.Release();
            }
        }

        private async ValueTask FireAndForgetShutdownAsync()
        {
            try
            {
                await _transitionGate.WaitAsync();
                try
                {
                    await PopAllAsync(ModeExitReason.Shutdown);
                }
                finally
                {
                    _transitionGate.Release();
                }
            }
            catch (Exception e)
            {
                _logger?.Send(MessageType.Exception, $"Shutdown cleanup failed: {e}");
            }
        }

        /// <summary>
        /// 積まれた遷移要求を、連鎖(Pushの中からさらにPush等)を含めて順に適用する.
        /// </summary>
        private async ValueTask ApplyTransitionsAsync(ModeTransitionRequestSink.Request[] initial, CancellationToken cancellationToken)
        {
            await _transitionGate.WaitAsync();
            try
            {
                var queue = new Queue<ModeTransitionRequestSink.Request>(initial);
                var cascadeDepth = 0;

                while (queue.Count > 0)
                {
                    if (Volatile.Read(ref _disposedState) != 0)
                    {
                        queue.Clear();
                        break;
                    }

                    if (++cascadeDepth > MaxCascadeDepth)
                    {
                        _logger?.Send(MessageType.Warning, $"Mode transition cascade exceeded {MaxCascadeDepth} levels. Remaining requests are discarded.");
                        break;
                    }

                    var request = queue.Dequeue();

                    if (!ReferenceEquals(_stack.Current, request.ExpectedTop))
                    {
                        _logger?.Send(MessageType.Warning, $"A mode transition ({request.Kind}) was requested against a stale mode. Discarded.");
                        continue;
                    }

                    switch (request.Kind)
                    {
                        case ModeTransitionRequestSink.RequestKind.Push:
                            await ApplyPushAsync(request.Mode, queue, cancellationToken);
                            break;

                        case ModeTransitionRequestSink.RequestKind.Replace:
                            await ApplyReplaceAsync(request.Mode, queue, cancellationToken);
                            break;

                        case ModeTransitionRequestSink.RequestKind.Pop:
                        {
                            var count = Math.Min(request.Count, _stack.Depth - 1);
                            for (var i = 0; i < count; i++)
                            {
                                await PopOneAsync(ModeExitReason.Popped);
                            }

                            break;
                        }
                    }
                }
            }
            finally
            {
                _transitionGate.Release();
            }
        }

        private async ValueTask ApplyPushAsync(ITerminalMode mode, Queue<ModeTransitionRequestSink.Request> queue, CancellationToken cancellationToken)
        {
            var context = BuildContextFor(mode);
            var turnId = _sink.BeginTurn(mode);
            try
            {
                await mode.OnEnterAsync(context, cancellationToken);
            }
            catch (Exception e)
            {
                _sink.Abort(turnId);
                _logger?.Send(MessageType.Exception, $"{mode.GetType().Name}.OnEnterAsync threw: {e}");
                await SafeExitAsync(mode, ModeExitReason.EnterFailed);
                return;
            }

            _stack.Push(mode, context);
            foreach (var follow in _sink.EndTurn(turnId))
            {
                queue.Enqueue(follow);
            }
        }

        private async ValueTask ApplyReplaceAsync(ITerminalMode mode, Queue<ModeTransitionRequestSink.Request> queue, CancellationToken cancellationToken)
        {
            if (_stack.Depth <= 1)
            {
                // 最下段(NormalMode)はReplaceで置き換えられない(常に非空・最下段固定の不変条件).
                // mode はまだ入場していないので OnEnterAsync は呼ばない.
                _logger?.Send(MessageType.Warning, $"Replace was requested at the root frame and is not allowed. Use Push instead. (mode: {mode.GetType().Name})");
                return;
            }

            var context = BuildContextFor(mode);
            var turnId = _sink.BeginTurn(mode);
            try
            {
                await mode.OnEnterAsync(context, cancellationToken);
            }
            catch (Exception e)
            {
                _sink.Abort(turnId);
                _logger?.Send(MessageType.Exception, $"{mode.GetType().Name}.OnEnterAsync threw: {e}");
                await SafeExitAsync(mode, ModeExitReason.EnterFailed);
                return;
            }

            var old = _stack.Current;
            if (!_stack.TryReplace(mode, context))
            {
                // OnEnterAsync実行中にPopAllAsync等で深さ1まで縮んだ場合のフォールバック.
                _sink.Abort(turnId);
                _logger?.Send(MessageType.Warning, $"Replace was discarded because the stack shrank to the root frame during OnEnterAsync. (mode: {mode.GetType().Name})");
                await SafeExitAsync(mode, ModeExitReason.EnterFailed);
                return;
            }

            await SafeExitAsync(old, ModeExitReason.Replaced);

            foreach (var follow in _sink.EndTurn(turnId))
            {
                queue.Enqueue(follow);
            }
        }

        private async ValueTask PopOneAsync(ModeExitReason reason)
        {
            var popped = _stack.Pop();
            if (popped is null)
            {
                return;
            }

            await SafeExitAsync(popped, reason);
        }

        private async ValueTask PopAllAsync(ModeExitReason reason)
        {
            while (_stack.Depth > 1)
            {
                await PopOneAsync(reason);
            }
        }

        private async ValueTask SafeExitAsync(ITerminalMode mode, ModeExitReason reason)
        {
            try
            {
                await mode.OnExitAsync(reason);
            }
            catch (Exception e)
            {
                _logger?.Send(MessageType.Exception, $"{mode.GetType().Name}.OnExitAsync threw: {e}");
            }
        }

        private IModeContext BuildContextFor(ITerminalMode mode)
        {
            var commands = _binder.BindFor(mode);
            return new ModeContext(commands, _output, _sink, _stackInspector);
        }

        /// <summary>
        /// 【共通後処理】発生した例外のログ出力を一括ハンドリングします。
        /// </summary>
        private void HandleException(Exception e)
        {
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

        /// <summary>
        /// <see cref="IModeStackInspector"/> への薄い委譲(循環参照を避けるため内部クラスにする).
        /// </summary>
        private sealed class StackInspector : IModeStackInspector
        {
            private readonly ExecuteCommandUseCase _owner;

            private StackInspector(ExecuteCommandUseCase owner) => _owner = owner;

            public static StackInspector From(ExecuteCommandUseCase owner) => new(owner);

            int IModeStackInspector.Depth => _owner._stack.Depth;

            IReadOnlyList<ModeStackFrameInfo> IModeStackInspector.Snapshot() => _owner._stack.Snapshot();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// アプリケーションの実行に必要なオブジェクト群を保持するコンテナ.
    /// Installerによって構築され、EntryPointに渡される.
    /// </summary>
    public sealed class TerminalRuntimeScope : IDisposable, IAsyncDisposable
    {
        public TerminalEntryPoint EntryPoint { get; }
        public ITerminalService Service { get; }
        public ICommandRegistry Registry { get; }
        public ICommandAutocomplete Autocomplete { get; }
        /// <summary>
        /// ウィンドウ全体の表示制御用View.
        /// <see cref="NullInstaller"/>ではNull Objectパターンの実装
        /// （<see cref="NullTerminalView"/>）が設定されるため、常に非null.
        /// </summary>
        public ITerminalView View { get; }

        private readonly IReadOnlyList<IDisposable> _disposables;
        private readonly IReadOnlyList<IAsyncDisposable> _asyncDisposables;
        private readonly ICommandLogger _logger;
        private bool _disposed;

        public TerminalRuntimeScope(
            TerminalEntryPoint entryPoint,
            ITerminalService service,
            ICommandRegistry registry,
            ICommandAutocomplete autocomplete,
            ITerminalView view,
            IReadOnlyList<IDisposable> disposables,
            IReadOnlyList<IAsyncDisposable> asyncDisposables = null,
            ICommandLogger logger = null)
        {
            EntryPoint = entryPoint;
            Service = service;
            Registry = registry;
            Autocomplete = autocomplete;
            View = view;
            _disposables = disposables ?? new List<IDisposable>(0);
            _asyncDisposables = asyncDisposables ?? new List<IAsyncDisposable>(0);
            _logger = logger;
        }

        /// <summary>
        /// 同期経路での破棄(Unityの<c>OnDestroy</c>等、非同期を許容できない箇所からのフォールバック).
        /// </summary>
        /// <remarks>
        /// 完走を保証しない(ログのみ)。<see cref="IAsyncDisposable"/>のみを実装するコンポーネント
        /// (同期<see cref="IDisposable"/>を実装していないもの)は、この経路では破棄できないため
        /// 警告ログに残すのみに留める。個々の破棄で発生した例外も同様にログのみとし、例外は投げない
        /// (Unityの<c>OnDestroy</c>から例外を投げると、以後の破棄処理やシーン破棄そのものを止めてしまうため)。
        /// 完全な破棄と例外の伝播を保証したい場合は <see cref="DisposeAsync"/> を使うこと.
        /// </remarks>
        void IDisposable.Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _asyncDisposables.Count; i++)
            {
                if (_asyncDisposables[i] is IDisposable syncDisposable)
                {
                    try
                    {
                        syncDisposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        _logger?.Send(MessageType.Exception, $"Failed to dispose '{_asyncDisposables[i].GetType().FullName}': {e}");
                    }
                }
                else
                {
                    _logger?.Send(
                        MessageType.Warning,
                        $"'{_asyncDisposables[i].GetType().FullName}' implements only IAsyncDisposable and could not be disposed synchronously. " +
                        "Call DisposeAsync() (or the equivalent async shutdown entry point) instead of the synchronous Dispose().");
                }
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; i++)
            {
                try
                {
                    _disposables[i]?.Dispose();
                }
                catch (Exception e)
                {
                    _logger?.Send(MessageType.Exception, $"Failed to dispose '{_disposables[i]?.GetType().FullName}': {e}");
                }
            }
        }

        /// <summary>
        /// 非同期経路での破棄. 全ての<see cref="IAsyncDisposable"/>コンポーネントの完走を待ってから
        /// 同期コンポーネントを破棄する(モードの<c>OnExitAsync</c>連鎖を完走させたい場合はこちらを使う).
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            List<Exception> exceptions = null;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _asyncDisposables.Count; i++)
            {
                try
                {
                    // Unity APIをOnExitAsync等の中で呼ぶモード実装を許容するため、
                    // ConfigureAwait(false)は使わない(呼び出し元のSynchronizationContextに戻す).
                    if (_asyncDisposables[i] != null)
                    {
                        await _asyncDisposables[i].DisposeAsync();
                    }
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; i++)
            {
                try
                {
                    _disposables[i]?.Dispose();
                }
                catch (Exception e)
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(e);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException("One or more exceptions occurred while disposing resources.", exceptions);
            }
        }
    }
}

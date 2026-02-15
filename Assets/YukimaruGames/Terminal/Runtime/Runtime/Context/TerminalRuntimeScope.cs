using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.Interface;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// アプリケーションの実行に必要なオブジェクト群を保持するコンテナ.
    /// Installerによって構築され、EntryPointに渡される.
    /// </summary>
    public class TerminalRuntimeScope : IDisposable
    {
        public TerminalEntryPoint EntryPoint { get; }
        public ITerminalService Service { get; }
        public ICommandRegistry Registry { get; }
        public ICommandAutocomplete Autocomplete { get; }

        private readonly IReadOnlyList<IDisposable> _disposables;

        public TerminalRuntimeScope(
            TerminalEntryPoint entryPoint,
            ITerminalService service,
            ICommandRegistry registry,
            ICommandAutocomplete autocomplete,
            IReadOnlyList<IDisposable> disposables)
        {
            EntryPoint = entryPoint;
            Service = service;
            Registry = registry;
            Autocomplete = autocomplete;
            _disposables = disposables ?? new List<IDisposable>(0);
        }

        void IDisposable.Dispose()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; i++)
            {
                _disposables[i]?.Dispose();
            }
        }
    }
}

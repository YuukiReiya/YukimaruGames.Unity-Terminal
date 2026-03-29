using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.API.Commands;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// アプリケーションの実行に必要なオブジェクト群を保持するコンテナ.
    /// Installerによって構築され、EntryPointに渡される.
    /// </summary>
    public sealed class TerminalRuntimeScope : IDisposable
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
            List<Exception> exceptions = null;
            
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
                throw new AggregateException($"One or more exceptions occurred while disposing resources.", exceptions);
            }
        }
    }
}

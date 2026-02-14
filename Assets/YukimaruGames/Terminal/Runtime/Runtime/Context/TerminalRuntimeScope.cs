using System;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// アプリケーションの実行に必要なオブジェクト群を保持するコンテナ.
    /// Installerによって構築され、EntryPointに渡される.
    /// </summary>
    public class TerminalRuntimeScope : IDisposable
    {
        public TerminalEntryPoint EntryPoint { get; }
        public TerminalCoordinator Coordinator { get; }
        public ITerminalService Service { get; }
        public ICommandRegistry Registry { get; }
        public ICommandAutocomplete Autocomplete { get; }

        public TerminalRuntimeScope(
            TerminalEntryPoint entryPoint, 
            TerminalCoordinator coordinator,
            ITerminalService service,
            ICommandRegistry registry,
            ICommandAutocomplete autocomplete)
        {
            EntryPoint = entryPoint;
            Coordinator = coordinator;
            Service = service;
            Registry = registry;
            Autocomplete = autocomplete;
        }

        public void Dispose()
        {
            (EntryPoint as IDisposable)?.Dispose();
            Coordinator?.Dispose();
        }
    }
}

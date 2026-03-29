using System;
using System.Linq;
using YukimaruGames.Terminal.Application.Core;
using YukimaruGames.Terminal.Domain.Core.Commands;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable, HideInTypeMenu, AddTypeMenu("None(null)")]
    public sealed class TerminalNullInstaller : IInstaller
    {
        TerminalRuntimeScope IInstaller.Install()
        {
            var logger = new CommandLogger(0);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var autocomplete = new CommandAutocomplete();
            var entryPoint = new TerminalEntryPoint(null, InputKeyboardType.None, null);
            var disposables = new object[]
            {
                logger,
                registry,
                invoker,
                parser,
                history,
                autocomplete,
                entryPoint,
            }.OfType<IDisposable>().ToArray();
            var service = new TerminalService(
                logger,
                registry,
                invoker,
                parser,
                history,
                autocomplete);
            return new TerminalRuntimeScope(
                entryPoint,
                service,
                registry,
                autocomplete,
                disposables);
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            (scope as IDisposable)?.Dispose();
        }
    }
}
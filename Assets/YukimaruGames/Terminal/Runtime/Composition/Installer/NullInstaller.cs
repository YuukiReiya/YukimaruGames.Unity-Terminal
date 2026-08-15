using System;
using System.Linq;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Repositories;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    [Serializable, HideInTypeMenu, AddTypeMenu("None(null)")]
    public sealed class NullInstaller : IInstaller
    {
        TerminalRuntimeScope IInstaller.Install()
        {
            var logger = new CommandLogger(0);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var autocomplete = new CommandAutocomplete();
            var normalMode = new ExecutionMode(logger, registry, invoker, parser, history, autocomplete);
            var executeCommandUseCase = new ExecuteCommandUseCase(logger, normalMode);
            var entryPoint = new TerminalEntryPoint(Array.Empty<IStartable>(), Array.Empty<IUpdatable>(), null);
            var components = new object[]
            {
                logger,
                registry,
                invoker,
                parser,
                history,
                autocomplete,
                executeCommandUseCase,
                entryPoint,
            };
            var asyncDisposables = components.OfType<IAsyncDisposable>().ToArray();
            var disposables = components.OfType<IDisposable>().Where(d => d is not IAsyncDisposable).ToArray();
            var service = new TerminalService(
                logger,
                registry,
                autocomplete,
                executeCommandUseCase);
            return new TerminalRuntimeScope(
                entryPoint,
                service,
                registry,
                autocomplete,
                new NullTerminalView(),
                disposables,
                asyncDisposables);
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            (scope as IDisposable)?.Dispose();
        }

        async ValueTask IInstaller.UninstallAsync(TerminalRuntimeScope scope)
        {
            if (scope is null) return;
            await ((IAsyncDisposable)scope).DisposeAsync();
        }

        public void Resolve(TerminalRuntimeScope scope)
        {
            // NullInstallerには再解決する設定がないため、何もしない
        }
    }
}

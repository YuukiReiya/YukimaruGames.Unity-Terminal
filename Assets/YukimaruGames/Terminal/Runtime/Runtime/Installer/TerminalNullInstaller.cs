using System;
using YukimaruGames.Terminal.Application.Core;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
{
    [Serializable, HideInTypeMenu, AddTypeMenu("None(null)")]
    public sealed class TerminalNullInstaller : IInstaller
    {
        TerminalRuntimeScope IInstaller.Install()
        {
            var entryPoint = new TerminalEntryPoint(null, InputKeyboardType.None, null);
            var service = new TerminalService(
                null,
                null,
                null,
                null,
                null,
                null);
            return new TerminalRuntimeScope(
                entryPoint,
                service,
                null,
                null,
                Array.Empty<IDisposable>());
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            (scope as IDisposable)?.Dispose();
        }
    }
}
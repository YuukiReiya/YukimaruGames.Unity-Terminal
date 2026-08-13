using System.Collections.Generic;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Services;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// ドメイン層のパラメータをとりまとめたContext.
    /// </summary>
    /// <remarks>
    /// <see cref="DomainBuilder"/>が構築し、<see cref="InstallerBase"/>の拡張点
    /// (<c>BuildBackend</c>)へ渡される。UIバックエンドの有無によらず全Installerで共通.
    /// </remarks>
    public struct DomainContext
    {
        /// <summary>
        /// 構成データ
        /// </summary>
        public IReadOnlyList<object> Components { get; set; }

        /// <inheritdoc cref="ITerminalService"/>
        public ITerminalService Service { get; set; }
        /// <inheritdoc cref="ICommandLogger"/>
        public ICommandLogger Logger { get; set; }
        /// <inheritdoc cref="ICommandHistory"/>
        public ICommandHistory History { get; set; }
        /// <inheritdoc cref="ICommandRegistry"/>
        public ICommandRegistry Registry { get; set; }
        /// <inheritdoc cref="ICommandAutocomplete"/>
        public ICommandAutocomplete Autocomplete { get; set; }
        /// <inheritdoc cref="ICommandDiscoverer"/>
        public ICommandDiscoverer Discoverer { get; set; }
        /// <inheritdoc cref="IExecuteCommandUseCase"/>
        public IExecuteCommandUseCase UseCase { get; set; }

        /// <summary>
        /// 既定モード.
        /// </summary>
        /// <remarks>
        /// <c>ITerminalMode.Prompt</c>はget-onlyで、プロンプト文字列を設定できるのは具象の
        /// <see cref="NormalMode"/>だけのため、インターフェースではなく具象を保持する.
        /// </remarks>
        public NormalMode Mode { get; set; }
    }
}

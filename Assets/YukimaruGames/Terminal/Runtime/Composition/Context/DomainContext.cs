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
        public IReadOnlyList<object> Components;

        /// <inheritdoc cref="ITerminalService"/>
        public ITerminalService Service;
        /// <inheritdoc cref="ICommandLogger"/>
        public ICommandLogger Logger;
        /// <inheritdoc cref="ICommandHistory"/>
        public ICommandHistory History;
        /// <inheritdoc cref="ICommandRegistry"/>
        public ICommandRegistry Registry;
        /// <inheritdoc cref="ICommandAutocomplete"/>
        public ICommandAutocomplete Autocomplete;
        /// <inheritdoc cref="ICommandDiscoverer"/>
        public ICommandDiscoverer Discoverer;
        /// <inheritdoc cref="IExecuteCommandUseCase"/>
        public IExecuteCommandUseCase UseCase;

        /// <summary>
        /// 既定モード.
        /// </summary>
        /// <remarks>
        /// <c>ITerminalMode.Prompt</c>はget-onlyで、プロンプト文字列を設定できるのは具象の
        /// <see cref="NormalMode"/>だけのため、インターフェースではなく具象を保持する.
        /// </remarks>
        public NormalMode Mode;
    }
}

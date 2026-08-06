using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services
{
    /// <summary>
    /// コマンドを検出するためのインターフェイス.
    /// </summary>
    public interface ICommandDiscoverer
    {
        /// <summary>
        /// 指定されたAssemblyからコマンドを検出.
        /// </summary>
        /// <returns>検出したコマンドハンドラーのコレクション</returns>
        IEnumerable<CommandSpecification> Discover();

        /// <summary>
        /// 指定したモード型に対する [TerminalModeCommand] を検出する.
        /// </summary>
        /// <remarks>
        /// アセンブリ全体を走査するのではなく、<paramref name="modeType"/>の継承チェーンを
        /// <c>BaseType</c>で直接辿るため、独自asmdefに置かれたモードでも発見できる。
        /// 基底クラスに宣言されたコマンドも継承先に引き継がれる(overrideされたメソッドは
        /// 派生側の属性が優先される)。
        /// </remarks>
        /// <param name="modeType">対象モードの実行時型</param>
        /// <param name="modeId">対象モードの識別子(文字列指定の属性とのマッチに使用)</param>
        /// <returns>検出したコマンドの設計情報</returns>
        IReadOnlyList<CommandSpecification> DiscoverModeCommands(Type modeType, string modeId);
    }
}

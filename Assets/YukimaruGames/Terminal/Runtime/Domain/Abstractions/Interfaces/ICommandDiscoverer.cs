using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Abstractions.Models;

namespace YukimaruGames.Terminal.Domain.Abstractions.Interfaces
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
    }
}

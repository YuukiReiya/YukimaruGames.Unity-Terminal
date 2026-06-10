using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services
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

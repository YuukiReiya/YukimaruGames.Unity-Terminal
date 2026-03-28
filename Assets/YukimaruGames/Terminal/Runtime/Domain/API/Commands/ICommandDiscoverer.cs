using System.Collections.Generic;

namespace YukimaruGames.Terminal.Domain.API.Commands
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

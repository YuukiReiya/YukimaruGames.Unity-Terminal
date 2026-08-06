using System;
using YukimaruGames.Terminal.Domain.Contracts.Models;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;

namespace YukimaruGames.Terminal.Domain.Contracts.Attributes
{
    /// <summary>
    /// コマンド登録のためのカスタム属性.
    /// </summary>
    /// <remarks>
    /// <c>sealed</c>にしていないのは、これを継承した独自属性(ラッパー属性)を利用者が
    /// 定義できるようにするため。<c>CommandDiscoverer</c>の属性探索は多態的
    /// (<c>Attribute.GetCustomAttribute</c>は派生型も一致と見なす)なので、
    /// 継承するだけで自動発見の対象になる。継承していない独自属性はサポート対象外.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class TerminalCommandAttribute : Attribute
    {
        /// <summary>
        /// メタ情報.
        /// </summary>
        public CommandMeta Meta { get; private set; }
        
        public TerminalCommandAttribute(string command, int maxArgCount = 0, int minArgCount = -1, string help = "")
        {
            Meta = new CommandMeta(command, maxArgCount, minArgCount, help);
        }
    }
}

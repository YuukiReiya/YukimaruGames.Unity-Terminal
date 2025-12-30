using System;
using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Domain.Attribute
{
    /// <summary>
    /// コマンド登録のためのカスタム属性.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TerminalCommandAttribute : System.Attribute
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

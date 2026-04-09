using System;
using System.Collections.Generic;
using System.Reflection;
using YukimaruGames.Terminal.Domain.Abstractions.Attributes;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces;
using YukimaruGames.Terminal.Domain.Abstractions.Models;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Services
{
    /// <summary>
    /// 登録されたコマンドの保存クラス.
    /// </summary>
    public sealed class CommandRegistry : ICommandRegistry
    {
        /// <summary>
        /// コマンドキャッシュ.
        /// </summary>
        private readonly Dictionary<string /* コマンドのエイリアス */, CommandHandler /* ハンドル */> _commands =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ログ発行.
        /// </summary>
        private readonly ICommandLogger _logger;
        
        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="logger">ロガーインスタンス.</param>
        public CommandRegistry(ICommandLogger logger) => _logger = logger;
        
        /// <summary>
        /// コマンドの追加.
        /// </summary>
        /// <param name="command">コマンド名</param>
        /// <param name="handle">コマンドのハンドル</param>
        public bool Add(string command, CommandHandler handle)
        {
            if (_commands.TryAdd(command, handle)) return true;
            _logger?.Send(MessageType.Error, $"Command '{command}' is already defined.");
            return false;
        }

        /// <summary>
        /// コマンドの追加.
        /// </summary>
        /// <param name="command">コマンド名</param>
        /// <param name="methodInfo">メソッド情報</param>
        /// <param name="attribute">属性</param>
        private void Add(string command, MethodInfo methodInfo, TerminalCommandAttribute attribute)
        {
            var proc = (CommandDelegate)Delegate.CreateDelegate(typeof(CommandDelegate), methodInfo);
            Add(command, new CommandHandler(
                proc,
                command,
                attribute.Meta.MinArgCount,
                attribute.Meta.MaxArgCount,
                attribute.Meta.Help
            ));
        }

        /// <summary>
        /// コマンドの削除.
        /// </summary>
        /// <param name="command">コマンドのエイリアス</param>
        public bool Remove(string command)
        {
            if (_commands.ContainsKey(command))
            {
                _commands.Remove(command);
                return true;
            }

            _logger?.Send(MessageType.Error, $"Command '{command}' is not registered.");
            return false;
        }
        
        /// <inheritdoc/> 
        public bool TryGet(string command, out CommandHandler handler)
        {
            if (_commands.TryGetValue(command, out handler))
            {
                return true;
            }
            return false;
        } 
    }
}
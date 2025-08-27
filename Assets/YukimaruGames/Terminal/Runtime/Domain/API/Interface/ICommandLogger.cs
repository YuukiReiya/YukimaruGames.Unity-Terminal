using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Interface
{
    /// <summary>
    /// コマンドのログ発行インターフェイス.
    /// </summary>
    public interface ICommandLogger
    {
        /// <summary>
        /// 最大ログ数.
        /// </summary>
        int MaxLogs { get; }
        
        /// <summary>
        /// ログ.
        /// </summary>
        IReadOnlyCollection<CommandLog> Logs { get; }

        /// <summary>
        /// ログが更新(追加・削除問わず)された時の通知.
        /// </summary>
        event Action OnItemUpdated;
        
        /// <summary>
        /// ログが追加された時の通知.
        /// </summary>
        event Action<CommandLog[]> OnItemAdded;
        
        /// <summary>
        /// ログが削除された時の通知. 
        /// </summary>
        event Action<CommandLog[]> OnItemRemoved; 
        
        /// <summary>
        /// クリア.
        /// </summary>
        public void Clear();
        
        /// <summary>
        /// 外部からのログ出力要求. 
        /// </summary>
        /// <param name="msgType">ログ出力種別</param>
        /// <param name="message">出力文字列</param>
        /// <remarks>
        /// 継承先で適切に出力先の処理をルーティングする.
        /// </remarks>
        public void Send(MessageType msgType, string message);
    }
}

using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Service
{
    /// <summary>
    /// コマンドのログ発行クラス.
    /// </summary>
    public class CommandLogger : ICommandLogger, IDisposable
    {
        /// <summary>
        /// バッファーに保存する最大数.
        /// (=バッファサイズ)
        /// </summary>
        /// <remarks>
        /// 最大数を超過したら古いものから削除される.
        /// </remarks>
        public int MaxLogs { get; }
        
        /// <summary>
        /// ログを保存しておくバッファー.
        /// </summary>
        private readonly Queue<CommandLog> _buffer;

        /// <summary>
        /// ログ更新コールバック.
        /// </summary>
        private Action _onItemUpdated;
        
        /// <summary>
        /// ログ追加コールバック.
        /// </summary>
        private Action<CommandLog[]> _onItemAdded;
        
        /// <summary>
        /// ログ削除コールバック.
        /// </summary>
        private Action<CommandLog[]> _onItemRemoved;
        
        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="maxLogs">ログを保存しておく上限</param>
        public CommandLogger(int maxLogs)
        {
            MaxLogs = maxLogs;
            _buffer = new Queue<CommandLog>(maxLogs);
        }

        /// <summary>
        /// 保存されたログ.
        /// </summary>
        public IReadOnlyCollection<CommandLog> Logs => _buffer;

        /// <inheritdoc/>
        /// <remarks>
        /// <p>Queueで管理している<see cref="_buffer"/>の追加・削除のいずれかが呼び出された際に通知する。</p>
        /// <p>追加・削除が同時に行われても一度だけの呼び出し.</p>
        /// </remarks>
        public event Action OnItemUpdated
        {
            add => _onItemUpdated += value;
            remove => _onItemUpdated -= value;
        }
        
        /// <inheritdoc/>
        public event Action<CommandLog[]> OnItemAdded
        {
            add => _onItemAdded += value;
            remove => _onItemAdded -= value;
        }
        
        /// <inheritdoc/>
        public event Action<CommandLog[]> OnItemRemoved
        {
            add => _onItemRemoved += value;
            remove => _onItemRemoved -= value;
        }

        /// <summary>
        /// ログの追加.
        /// </summary>
        private void Add(MessageType type, string message)
        {
            var id = _buffer.Count + 1;
            if (MaxLogs < _buffer.Count + 1)
            {
                var item = _buffer.Dequeue();
                _onItemRemoved?.Invoke(new[] { item });
                id = item.Id;
            }

            {
                var item = new CommandLog(id, type, DateTimeOffset.Now, message);
                _buffer.Enqueue(item);
                _onItemAdded?.Invoke(new[] { item });
            }
            
            _onItemUpdated?.Invoke();
        }

        /// <summary>
        /// ログのクリア.
        /// </summary>
        public void Clear()
        {
            if (_buffer.Count == 0) return;

            var array = _buffer.ToArray();
            _buffer.Clear();
            _onItemRemoved?.Invoke(array);
            _onItemUpdated?.Invoke();
        }

        /// <inheritdoc/>
        public void Send(MessageType msgType, string message) => Add(msgType, message);

        /// <inheritdoc/>
        public void Dispose()
        {
            _onItemAdded = null;
            _onItemRemoved = null;
            _onItemUpdated = null;
        }
    }
}

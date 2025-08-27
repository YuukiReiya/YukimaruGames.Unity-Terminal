using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Interface;

namespace YukimaruGames.Terminal.Domain.Service
{
    /// <summary>
    /// 入力コマンドの履歴管理クラス.
    /// </summary>
    public sealed class CommandHistory : ICommandHistory
    {
        /// <summary>
        /// 入力されたコマンドを保持.
        /// </summary>
        private readonly List<string> _histories = new();
        
        /// <summary>
        /// 現在参照中の履歴位置のインデックス.
        /// </summary>
        private int _index;

        /// <summary>
        /// 空メッセージ.
        /// </summary>
        /// <remarks>
        /// 次/前の履歴を返す時に存在しなかった際に返す.
        /// </remarks>
        private string _emptyMessage;
        private string EmptyMessage => _emptyMessage ??= string.Empty;

        public IReadOnlyCollection<string> Histories => _histories;

        /// <summary>
        /// 登録内容のクリアと参照インデックスのリセット.
        /// </summary>
        public void Clear()
        {
            _histories.Clear();
            _index = 0;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// null or 空文字なら登録しない.
        /// </remarks>
        public bool Add(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }
            
            _histories.Add(str);
            
            // MEMO:
            // 更新と同時に位置を戻す.
            _index = _histories.Count;
            return true;
        }

        /// <inheritdoc/>
        /// <returns>
        /// 配列の参照外であれば'EmptyMessage'を返す.
        /// </returns>
        public string Next()
        {
            _index++;
            if (_histories.Count <= _index)
            {
                _index = _histories.Count;
                return EmptyMessage;
            }

            _index = System.Math.Max(0, System.Math.Min(_histories.Count - 1, _index));
            return _histories[_index];
        }

        /// <inheritdoc/>
        /// <returns>
        /// 配列の参照外であれば'EmptyMessage'を返す.
        /// </returns>
        public string Previous()
        {
            if (_histories.Count == 0)
            {
                return EmptyMessage; 
            }

            _index = System.Math.Max(0, System.Math.Min(_histories.Count - 1, --_index));
            return _histories[_index];
        }
    }
}
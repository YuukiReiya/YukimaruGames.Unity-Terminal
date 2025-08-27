using System.Collections.Generic;

namespace YukimaruGames.Terminal.Domain.Interface
{
    /// <summary>
    /// 入力コマンドの履歴管理インターフェイス.
    /// </summary>
    public interface ICommandHistory
    {
        /// <summary>
        /// 履歴.
        /// </summary>
        IReadOnlyCollection<string> Histories { get; }
        
        /// <summary>
        /// 登録内容のクリア.
        /// </summary>
        void Clear();

        /// <summary>
        /// 履歴の登録.
        /// </summary>
        /// <param name="str">登録文字列</param>
        /// <returns>登録の成否
        /// <p>true : 成功</p>
        /// <p>false : 失敗</p>
        /// </returns>
        bool Add(string str);
        
        /// <summary>
        /// 次の履歴を返す.
        /// </summary>
        string Next();
        
        /// <summary>
        /// 前の履歴を返す.
        /// </summary>
        string Previous();
    }
}

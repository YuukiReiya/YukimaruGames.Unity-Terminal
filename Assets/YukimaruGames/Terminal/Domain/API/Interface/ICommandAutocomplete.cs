using System.Collections.Generic;

namespace YukimaruGames.Terminal.Domain.Interface
{
    /// <summary>
    /// コマンドの自動補完インターフェイス.
    /// </summary>
    public interface ICommandAutocomplete
    {
        /// <summary>
        /// 登録ワード一覧.
        /// </summary>
        IEnumerable<string> KnownWords { get; }
        
        /// <summary>
        /// 補完先の登録.
        /// </summary>
        /// <param name="word">補完出来る文字列として登録する文字列</param>
        /// <returns>登録の成否
        /// <p>true : 成功</p>
        /// <p>false : 失敗</p>
        /// </returns>
        bool Register(string word);
        
        /// <summary>
        /// 補完.
        /// </summary>
        /// <param name="text">補完判定文字列</param>
        /// <returns>
        /// 補完可能なキーワード.
        /// </returns>
        string[] Complete(string text);
    }
}

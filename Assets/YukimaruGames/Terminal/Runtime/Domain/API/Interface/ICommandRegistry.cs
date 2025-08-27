using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Domain.Interface
{
    /// <summary>
    /// 登録されたコマンドの保存クラス.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// コマンドの追加.
        /// </summary>
        /// <param name="command">エイリアス名</param>
        /// <param name="handle">コマンドのハンドル</param>
        /// <returns>追加の成否
        /// <p>true : 成功</p>
        /// <p>false : 失敗</p>
        /// </returns>
        bool Add(string command, CommandHandler handle);
        
        /// <summary>
        /// コマンドの削除.
        /// </summary>
        /// <param name="command">エイリアス名</param>
        /// <returns>削除の成否
        /// <p>true : 成功</p>
        /// <p>false : 失敗</p>
        /// </returns>
        bool Remove(string command);

        /// <summary>
        /// コマンドの取得.
        /// </summary>
        /// <param name="command">コマンドの登録エイリアス</param>
        /// <param name="handler">ハンドル</param>
        /// <returns>
        /// true : 成功
        /// false: 失敗
        /// </returns>
        bool TryGet(string command, out CommandHandler handler);
    }
}

using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Presentation.Contracts
{
    /// <summary>
    /// ログ表示に関するView操作契約.
    /// <para>
    /// Phase 6（Adapters）でIMGUI等の具体的な描画実装から接続される想定の契約定義。
    /// </para>
    /// </summary>
    public interface ILogView
    {
        /// <summary>ログを1行追加する.</summary>
        void AppendLog(string message, TerminalColor color);

        /// <summary>ログを全てクリアする.</summary>
        void Clear();
    }
}

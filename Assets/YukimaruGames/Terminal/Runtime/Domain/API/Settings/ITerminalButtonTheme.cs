using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <summary>
    /// ターミナルのボタン配色に関する設定を提供します。
    /// </summary>
    public interface ITerminalButtonTheme
    {
        /// <summary>実行ボタンの色を取得します。</summary>
        TerminalColor Execute { get; }

        /// <summary>コピーボタンの色を取得します。</summary>
        TerminalColor Copy { get; }

        /// <summary>汎用ボタン（開閉ボタンなど）の基本色を取得します。</summary>
        TerminalColor Base { get; }
    }
}

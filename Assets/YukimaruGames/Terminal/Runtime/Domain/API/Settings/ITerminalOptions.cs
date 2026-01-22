using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <summary>
    /// ターミナルの動作、バッファ、入力方式など、機能的な設定を提供します。
    /// </summary>
    public interface ITerminalOptions
    {
        /// <summary>使用する入力キーボードのタイプを取得します。</summary>
        InputKeyboardType InputKeyboardType { get; }

        /// <summary>起動時のウィンドウ状態（開いているか閉じているか）を取得します。</summary>
        TerminalState BootupWindowState { get; }

        /// <summary>ウィンドウのアンカー位置（上・下など）を取得します。</summary>
        TerminalAnchor Anchor { get; }

        /// <summary>ウィンドウのスタイル（コンパクト、フルスクリーンなど）を取得します。</summary>
        TerminalWindowStyle WindowStyle { get; }

        /// <summary>コマンド履歴のバッファサイズを取得します。</summary>
        int BufferSize { get; }

        /// <summary>プロンプトに表示する記号（"$" など）を取得します。</summary>
        string Prompt { get; }

        /// <summary>起動時に自動実行するコマンドを取得します。</summary>
        string BootupCommand { get; }

        /// <summary>トグルボタンの可視状態を取得します。</summary>
        bool ButtonVisible { get; }

        /// <summary>トグルボタンの配置（反転するかどうか）を取得します。</summary>
        bool ButtonReverse { get; }
    }
}

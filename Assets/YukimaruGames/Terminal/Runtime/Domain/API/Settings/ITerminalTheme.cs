using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <summary>
    /// ターミナルの外観（色、フォント、アニメーション速度など）に関する設定を提供します。
    /// </summary>
    public interface ITerminalTheme
    {
        /// <summary>フォントサイズを取得します。</summary>
        int FontSize { get; }

        /// <summary>背景色を取得します。</summary>
        TerminalColor Background { get; }

        /// <summary>メッセージの基本文字色を取得します。</summary>
        TerminalColor Message { get; }

        /// <summary>ログのエントリ色を取得します。</summary>
        TerminalColor Entry { get; }

        /// <summary>警告メッセージの色を取得します。</summary>
        TerminalColor Warning { get; }

        /// <summary>エラーメッセージの色を取得します。</summary>
        TerminalColor Error { get; }

        /// <summary>アサート（断言）エラーの色を取得します。</summary>
        TerminalColor Assert { get; }

        /// <summary>例外エラーの色を取得します。</summary>
        TerminalColor Exception { get; }

        /// <summary>システムメッセージの色を取得します。</summary>
        TerminalColor System { get; }

        /// <summary>入力中のテキストの色を取得します。</summary>
        TerminalColor Input { get; }

        /// <summary>カーソルの色を取得します。</summary>
        TerminalColor Caret { get; }

        /// <summary>選択範囲の色を取得します。</summary>
        TerminalColor Selection { get; }

        /// <summary>プロンプト記号の色を取得します。</summary>
        TerminalColor Prompt { get; }

        /// <summary>カーソルの点滅速度を取得します。</summary>
        float CursorFlashSpeed { get; }

        /// <summary>ウィンドウの開閉アニメーションの時間を秒単位で取得します。</summary>
        float Duration { get; }

        /// <summary>コンパクト表示時のスケール値を取得します。</summary>
        float CompactScale { get; }
    }
}

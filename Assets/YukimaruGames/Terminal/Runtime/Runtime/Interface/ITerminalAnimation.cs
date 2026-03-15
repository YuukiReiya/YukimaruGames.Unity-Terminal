using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// ターミナルウィンドウのアニメーション・レイアウト設定を提供します。
    /// </summary>
    /// <remarks>
    /// 見た目（色・フォント）を担う <see cref="ITerminalTheme"/> とは独立した、
    /// ウィンドウの動作・配置に関するパラメータを定義します。
    /// </remarks>
    public interface ITerminalAnimation
    {
        /// <summary>起動時のウィンドウ状態を取得します。</summary>
        WindowState BootupWindowState { get; }

        /// <summary>ウィンドウの表示アンカー位置を取得します。</summary>
        WindowAnchor Anchor { get; }

        /// <summary>ウィンドウスタイルを取得します。</summary>
        WindowStyle WindowStyle { get; }

        /// <summary>アニメーションの再生時間（秒）を取得します。</summary>
        float Duration { get; }

        /// <summary>Compactスタイル時のウィンドウ縮小スケールを取得します。</summary>
        float CompactScale { get; }
    }
}

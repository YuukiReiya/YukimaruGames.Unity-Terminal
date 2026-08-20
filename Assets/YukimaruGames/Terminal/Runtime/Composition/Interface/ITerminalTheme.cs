using UnityEngine;

namespace YukimaruGames.Terminal.Composition
{
    public interface ITerminalTheme
    {
        Font Font { get; }

        /// <summary>
        /// フォントサイズ(px).
        /// </summary>
        /// <remarks>
        /// <see cref="ScaleFontWithScreen"/>が<c>true</c>のときは
        /// 「<see cref="ReferenceResolution"/>での大きさ」として扱われ、実際の描画サイズは
        /// 画面サイズに応じて拡縮される。<c>false</c>のときはこの値がそのまま使われる.
        /// </remarks>
        int FontSize { get; }

        /// <summary>
        /// 画面サイズに合わせてフォントサイズを拡縮するか.
        /// </summary>
        /// <remarks>
        /// ウィンドウは画面サイズに対する比率で開くため、拡縮しないと解像度によって
        /// 1画面に入る行数が変わる。<c>true</c>にすると、どの解像度でも行数と見た目の比率が
        /// 一定に保たれる(既定は<c>false</c>=従来どおり<see cref="FontSize"/>をそのまま使う).
        /// </remarks>
        bool ScaleFontWithScreen { get; }

        /// <summary>
        /// <see cref="FontSize"/>が想定している基準の解像度.
        /// </summary>
        /// <remarks>
        /// <see cref="ScaleFontWithScreen"/>が<c>true</c>のときにのみ使う。
        /// 現状の拡縮はウィンドウ高さに連動するため<b>高さのみ</b>を参照する
        /// (幅は将来の拡張余地として受け取っている).
        /// </remarks>
        Vector2Int ReferenceResolution { get; }
        Color BackgroundColor { get; }
        Color MessageColor { get; }
        Color EntryColor { get; }
        Color WarningColor { get; }
        Color ErrorColor { get; }
        Color AssertColor { get; }
        Color ExceptionColor { get; }
        Color SystemColor { get; }
        Color InputColor { get; }
        Color CaretColor { get; }
        Color SelectionColor { get; }
        Color PromptColor { get; }
        Color ExecuteButtonColor { get; }
        Color ButtonColor { get; }
        Color CopyButtonColor { get; }
        float CursorFlashSpeed { get; }
    }
}

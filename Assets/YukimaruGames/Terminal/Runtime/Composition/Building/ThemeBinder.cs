using System;
using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="ITerminalTheme"/>の値を、バックエンドに依存しないアクセサ
    /// (カラーパレット / キャレット点滅速度)へ結びつける.
    /// </summary>
    /// <remarks>
    /// <c>_theme</c>は各UIバックエンドが個別に宣言する(#145)。テーマを持つバックエンドが
    /// 複数あってもこの処理を重複させないため、静的ヘルパーとして切り出してある。
    ///
    /// 書き込みしか行わないため、引数は<c>Accessor</c>ではなく<see cref="IColorPaletteMutator"/> /
    /// <see cref="ICursorFlashSpeedMutator"/>を受け取る(このリポジトリのアクセサは
    /// <c>IXxxAccessor : IXxxMutator, IXxxProvider</c>の形で読み書きを分離しており、
    /// 利用側は必要な側だけに依存する).
    /// </remarks>
    public static class ThemeBinder
    {
        /// <summary>
        /// 画面の高さに合わせて、実際に描画へ渡すフォントサイズを求める.
        /// </summary>
        /// <remarks>
        /// <see cref="ITerminalTheme.ScaleFontWithScreen"/>が無効なら<see cref="ITerminalTheme.FontSize"/>を
        /// そのまま返す。有効な場合、テーマの値は絶対ピクセル値のため、そのまま使うと
        /// 画面が大きいほど「文字の大きさは同じで行数だけ増える」挙動になる。ウィンドウ自体は
        /// 画面サイズに対する比率で開くため、解像度によって1画面に入る行数が変わってしまう。
        /// <para>
        /// <see cref="ITerminalTheme.ReferenceResolution"/>に対する比率で拡縮することで、どの解像度でも
        /// 「ウィンドウに入る行数」と見た目の比率を一定に保つ。3バックエンドで同じ計算を使う
        /// ことで、解像度が変わっても表示が揃ったままになる.
        /// </para>
        /// </remarks>
        /// <param name="theme">フォントサイズと拡縮設定の供給元</param>
        /// <param name="screenHeight">現在の画面の高さ(px)</param>
        /// <exception cref="ArgumentNullException"><paramref name="theme"/>がnullの場合.</exception>
        public static int ResolveFontSize(ITerminalTheme theme, int screenHeight)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));

            // 拡縮しない設定なら、Inspectorに入れた値がそのまま描画サイズになる(既定).
            if (!theme.ScaleFontWithScreen) return theme.FontSize;

            var referenceHeight = theme.ReferenceResolution.y;

            // 基準・現在のいずれかが取得できない状況(初期化順・未設定)では拡縮せずに済ませる.
            if (referenceHeight <= 0 || screenHeight <= 0) return theme.FontSize;

            var scaled = Mathf.RoundToInt(theme.FontSize * (screenHeight / (float)referenceHeight));

            // 小さな画面でも0にはしない(0は「描画しない」と区別がつかず、原因の追いにくい不具合になる).
            return Mathf.Max(1, scaled);
        }

        /// <summary>
        /// テーマのログ種別色から<see cref="IColorPaletteAccessor"/>を生成する.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="theme"/>がnullの場合(値の供給元のため必須).
        /// </exception>
        public static IColorPaletteAccessor CreateColorPalette(ITerminalTheme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));

            return new ColorPaletteAccessor(new Dictionary<string, Color>
            {
                { Definitions.ThemeLabel.Message, theme.MessageColor },
                { Definitions.ThemeLabel.Entry, theme.EntryColor },
                { Definitions.ThemeLabel.Warning, theme.WarningColor },
                { Definitions.ThemeLabel.Error, theme.ErrorColor },
                { Definitions.ThemeLabel.Assert, theme.AssertColor },
                { Definitions.ThemeLabel.Exception, theme.ExceptionColor },
                { Definitions.ThemeLabel.System, theme.SystemColor },
                { Definitions.ThemeLabel.Cursor, theme.CaretColor },
                { Definitions.ThemeLabel.Selection, theme.SelectionColor },
            });
        }

        /// <summary>
        /// テーマの現在値をアクセサへ再適用する(Inspectorでの変更を反映する経路).
        /// </summary>
        /// <remarks>
        /// <paramref name="palette"/> / <paramref name="cursorFlash"/>は、構築途中の呼び出しを
        /// 許容するためnullを無視する(生成前のアクセサに対しては何もしない)。
        /// <paramref name="theme"/>は値の供給元なので必須。nullは「設定し忘れ」であって
        /// 「まだ無い」ではないため、握り潰さず例外にする
        /// (呼び出し側はNull Object実装(<see cref="NullTheme"/>)へ差し替えてから渡すこと).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="theme"/>がnullの場合.
        /// </exception>
        public static void Apply(ITerminalTheme theme, IColorPaletteMutator palette, ICursorFlashSpeedMutator cursorFlash)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));

            if (palette != null)
            {
                palette[Definitions.ThemeLabel.Message] = theme.MessageColor;
                palette[Definitions.ThemeLabel.Entry] = theme.EntryColor;
                palette[Definitions.ThemeLabel.Warning] = theme.WarningColor;
                palette[Definitions.ThemeLabel.Error] = theme.ErrorColor;
                palette[Definitions.ThemeLabel.Assert] = theme.AssertColor;
                palette[Definitions.ThemeLabel.Exception] = theme.ExceptionColor;
                palette[Definitions.ThemeLabel.System] = theme.SystemColor;
                palette[Definitions.ThemeLabel.Cursor] = theme.CaretColor;
                palette[Definitions.ThemeLabel.Selection] = theme.SelectionColor;
            }

            if (cursorFlash != null)
            {
                cursorFlash.FlashSpeed = theme.CursorFlashSpeed;
            }
        }
    }
}

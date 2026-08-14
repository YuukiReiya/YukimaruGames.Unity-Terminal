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
    internal static class ThemeBinder
    {
        /// <summary>
        /// テーマのログ種別色から<see cref="IColorPaletteAccessor"/>を生成する.
        /// </summary>
        internal static IColorPaletteAccessor CreateColorPalette(ITerminalTheme theme)
        {
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
        /// 構築途中の呼び出しも許容するため、各引数のnullは無視する.
        /// </remarks>
        internal static void Apply(ITerminalTheme theme, IColorPaletteMutator palette, ICursorFlashSpeedMutator cursorFlash)
        {
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

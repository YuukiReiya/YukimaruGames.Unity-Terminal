using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Presentation.Constants;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="ITerminalTheme"/>のうち、バックエンドに依存しない同期先
    /// (<see cref="ColorPaletteAccessor"/> / <see cref="CursorFlashSpeedAccessor"/>)への
    /// 適用をまとめる.
    /// </summary>
    /// <remarks>
    /// <c>_theme</c>は各UIバックエンドが個別に宣言する(#145)。テーマを持つバックエンドが
    /// 複数あってもこの同期処理を重複させないため、静的ヘルパーとして切り出してある.
    /// </remarks>
    internal static class ThemeSync
    {
        /// <summary>
        /// テーマのログ種別色から<see cref="ColorPaletteAccessor"/>を生成する.
        /// </summary>
        internal static ColorPaletteAccessor CreateColorPalette(ITerminalTheme theme)
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
        /// テーマの現在値を、生成済みのアクセサへ再適用する(Inspectorでの変更を反映する経路).
        /// </summary>
        /// <remarks>
        /// 構築途中の呼び出しも許容するため、各アクセサのnullは無視する.
        /// </remarks>
        internal static void Apply(ITerminalTheme theme, ColorPaletteAccessor palette, CursorFlashSpeedAccessor cursorFlash)
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

using System;
using UnityEngine;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// 最小限の設定値を持つ Null Object パターン実装.
    /// ユーザーが意図的に Theme を null にした場合のフォールバック先.
    /// </summary>
    [Serializable, HideInTypeMenu]
    public class TerminalNullTheme : ITerminalTheme
    {
        // Font: null (システムフォントにフォールバック)
        public Font Font => null;

        // 最小限のサイズ
        public int FontSize => 0;

        // モノクロームな配色
        public Color BackgroundColor => Color.clear;
        public Color MessageColor => Color.clear;
        public Color EntryColor => Color.clear;
        public Color WarningColor => Color.clear;
        public Color ErrorColor => Color.clear;
        public Color AssertColor => Color.clear;
        public Color ExceptionColor => Color.clear;
        public Color SystemColor => Color.clear;
        public Color InputColor => Color.clear;
        public Color CaretColor => Color.clear;
        public Color SelectionColor => Color.clear;
        public Color PromptColor => Color.clear;
        public Color ExecuteButtonColor => Color.clear;
        public Color ButtonColor => Color.clear;
        public Color CopyButtonColor => Color.clear;

        // 控えめなカーソル速度
        public float CursorFlashSpeed => 0.0f;
    }
}
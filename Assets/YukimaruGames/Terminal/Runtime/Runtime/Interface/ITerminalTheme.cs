using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime
{
    public interface ITerminalTheme
    {
        Font Font { get; }
        int FontSize { get; }
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
        
        TerminalState BootupWindowState { get; }
        TerminalAnchor Anchor { get; }
        TerminalWindowStyle WindowStyle { get; }
        float Duration { get; }
        float CompactScale { get; }
    }
}

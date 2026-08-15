#if TERMINAL_UGUI_AVAILABLE
using UnityEngine;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.UGUI;
using YukimaruGames.Terminal.Adapters.UGUI.Renderers;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="ITerminalTheme"/>を<see cref="WindowRoot"/>配下のuGUI要素へ適用する.
    /// </summary>
    /// <remarks>
    /// uGUIは<c>Text</c>・<c>Image</c>・<c>InputField</c>と対応が素直に取れるため、
    /// UIToolkit版のようなバックエンド固有のフォントサイズ既定値は持たず、
    /// <see cref="ITerminalTheme.FontSize"/>をそのまま使う
    /// (<c>CanvasScaler</c>を<c>ConstantPixelSize</c>/<c>scaleFactor=1</c>にしているため、
    /// IMGUI版の<c>GUIStyle.fontSize</c>と同じ生pxとして扱える).
    /// </remarks>
    internal sealed class UGUIThemeApplier
    {
        private readonly WindowRoot _windowRoot;

        /// <summary>
        /// ログ行のフォント同期先。生成順の都合でコンストラクタでは渡せないため、
        /// <see cref="UGUIInstaller"/>が生成後に設定する.
        /// </summary>
        internal LogRenderer LogRenderer { get; set; }

        internal UGUIThemeApplier(WindowRoot windowRoot) => _windowRoot = windowRoot;

        /// <summary>
        /// テーマ色・フォントをuGUI要素へ適用する.
        /// </summary>
        internal void Apply(ITerminalTheme theme)
        {
            if (_windowRoot == null || !_windowRoot.IsInitialized) return;

            var font = ResolveFont(theme);
            var fontSize = theme.FontSize;

            if (_windowRoot.RootBackground != null) _windowRoot.RootBackground.color = theme.BackgroundColor;
            if (_windowRoot.InputRowBackground != null) _windowRoot.InputRowBackground.color = theme.BackgroundColor;

            ApplyText(_windowRoot.PromptLabel, theme.PromptColor, font, fontSize);
            ApplyInputField(theme, font, fontSize);
            ApplyButton(_windowRoot.SubmitButton, theme.ExecuteButtonColor, font, fontSize);
            ApplyButton(_windowRoot.LauncherOpenButton, theme.ButtonColor, font, fontSize);
            ApplyButton(_windowRoot.LauncherCloseButton, theme.ButtonColor, font, fontSize);

            if (LogRenderer != null)
            {
                LogRenderer.CopyButtonColor = theme.CopyButtonColor;
                LogRenderer.Font = font;
                LogRenderer.FontSize = fontSize;
            }
        }

        /// <summary>
        /// テーマのフォントを解決する.
        /// </summary>
        /// <remarks>
        /// <c>Text.font</c>がnullだと文字が一切描画されない(#122でUIToolkit版が踏んだのと同じ)。
        /// 未指定時はプロジェクトのResourcesに頼らず、Unity組み込みの
        /// <c>LegacyRuntime.ttf</c>へフォールバックする.
        /// </remarks>
        private static Font ResolveFont(ITerminalTheme theme)
        {
            return theme.Font != null ? theme.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void ApplyText(Text text, Color color, Font font, int fontSize)
        {
            if (text == null) return;

            text.color = color;
            if (font != null) text.font = font;
            if (fontSize > 0) text.fontSize = fontSize;
        }

        private static void ApplyButton(Button button, Color color, Font font, int fontSize)
        {
            if (button == null) return;

            ApplyText(button.GetComponentInChildren<Text>(true), color, font, fontSize);
        }

        /// <summary>
        /// 入力欄へテーマを適用する.
        /// </summary>
        /// <remarks>
        /// キャレットの点滅は<c>CursorPresenter</c>/<c>CursorView</c>側で管理するため、
        /// <see cref="InputField.caretBlinkRate"/>を0にしてuGUIネイティブの点滅を止める
        /// (IMGUI版と同じ考え方).
        /// </remarks>
        private void ApplyInputField(ITerminalTheme theme, Font font, int fontSize)
        {
            var field = _windowRoot.InputField;
            if (field == null) return;

            ApplyText(field.textComponent, theme.InputColor, font, fontSize);

            field.customCaretColor = true;
            field.caretColor = theme.CaretColor;
            field.selectionColor = theme.SelectionColor;
            field.caretBlinkRate = 0f;
        }
    }
}
#endif

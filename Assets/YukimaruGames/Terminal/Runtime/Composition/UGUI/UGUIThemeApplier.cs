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

            // 実行ボタンはウィンドウ内(背景色が敷かれた入力行の上)にあるため透明でよい。
            // ランチャーの開閉ボタンはウィンドウ矩形の外側に配置されるため、透明にすると
            // ゲーム画面の上に文字だけが浮いてしまう。背景色を敷いてターミナルの一部に見せる.
            ApplyButton(_windowRoot.SubmitButton, theme.ExecuteButtonColor, Color.clear, font, fontSize);
            ApplyButton(_windowRoot.LauncherOpenButton, theme.ButtonColor, theme.BackgroundColor, font, fontSize);
            ApplyButton(_windowRoot.LauncherCloseButton, theme.ButtonColor, theme.BackgroundColor, font, fontSize);

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

        /// <summary>
        /// ボタンへテーマを適用する.
        /// </summary>
        /// <remarks>
        /// <c>Button</c>の既定の背景(白のUISprite)をそのままにするとuGUI版だけ白い箱が浮くため、
        /// 呼び出し側が背景色を明示する。<c>Image</c>自体は消さずに色だけ変える
        /// (消すとクリック判定が無くなる。アルファ0でもレイキャストは既定で通る).
        /// </remarks>
        /// <param name="color">ラベルの色.</param>
        /// <param name="backgroundColor">ボタン背景の色.</param>
        private static void ApplyButton(Button button, Color color, Color backgroundColor, Font font, int fontSize)
        {
            if (button == null) return;

            if (button.image != null) button.image.color = backgroundColor;

            ApplyText(button.GetComponentInChildren<Text>(true), color, font, fontSize);
        }

        /// <summary>
        /// 入力欄へテーマを適用する.
        /// </summary>
        /// <remarks>
        /// キャレットの点滅は<c>CursorPresenter</c>/<c>CursorView</c>側で管理するため、
        /// <see cref="InputField.caretBlinkRate"/>を0にしてuGUIネイティブの点滅を止める
        /// (IMGUI版と同じ考え方)。
        /// <para>
        /// 背景は透明にする。<see cref="InputField"/>の既定の背景(白のUISprite)をそのままにすると
        /// uGUI版だけ白い箱が浮き、テーマの背景色と噛み合わない。IMGUI版・UIToolkit版は入力欄に
        /// 独立した背景を持たず、ウィンドウ背景の上にプロンプトと文字が乗るだけの見た目のため、
        /// そちらへ揃える。<c>Image</c>自体は残す(消すとクリックでフォーカスできなくなる。
        /// アルファ0でもレイキャストは既定で通る).
        /// </para>
        /// </remarks>
        private void ApplyInputField(ITerminalTheme theme, Font font, int fontSize)
        {
            var field = _windowRoot.InputField;
            if (field == null) return;

            ApplyText(field.textComponent, theme.InputColor, font, fontSize);

            if (field.image != null) field.image.color = Color.clear;

            field.customCaretColor = true;
            field.caretColor = theme.CaretColor;
            field.selectionColor = theme.SelectionColor;
            field.caretBlinkRate = 0f;
        }
    }
}
#endif

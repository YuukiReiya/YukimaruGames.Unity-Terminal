#if TERMINAL_UITOOLKIT_AVAILABLE
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.UIToolkit;
using YukimaruGames.Terminal.Adapters.UIToolkit.Renderers;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="ITerminalTheme"/>と、UIToolkitバックエンド固有の表示設定を、
    /// <see cref="WindowRoot"/>配下の<see cref="VisualElement"/>へ適用する.
    /// </summary>
    /// <remarks>
    /// ImmediateModeInstaller(IMGUI)がGUIStyle経由で行っている色・フォントの適用に相当する
    /// 処理を、UIToolkit向けに肩代わりする。<see cref="UIToolkitInstaller"/>から分離してあるのは、
    /// 配線(Installer)と見た目の適用という別々の関心事を1クラスに混ぜないため(#137).
    /// </remarks>
    internal sealed class UIToolkitThemeApplier
    {
        private readonly WindowRoot _windowRoot;

        /// <summary>
        /// ログ行のフォント同期先。生成順の都合でコンストラクタでは渡せないため、
        /// <see cref="UIToolkitInstaller"/>が生成後に設定する.
        /// </summary>
        internal LogRenderer LogRenderer { get; set; }

        internal UIToolkitThemeApplier(WindowRoot windowRoot) => _windowRoot = windowRoot;

        /// <summary>
        /// テーマ色・フォントとスクロール設定をVisualElementへ適用する.
        /// </summary>
        /// <param name="theme">適用するテーマ</param>
        /// <param name="scrollSensitivity">マウスホイール1クリックあたりのスクロール量(px)</param>
        /// <param name="scrollDecelerationRate">慣性スクロールの減速率</param>
        internal void Apply(ITerminalTheme theme, float scrollSensitivity, float scrollDecelerationRate)
        {
            if (_windowRoot == null || !_windowRoot.IsInitialized) return;

            var fontDefinition = ResolveFontDefinition(theme);

            // フォントサイズはIMGUI版・uGUI版と同じくテーマの値をそのまま使う。
            // かつては「IMGUI向けに調整された値をUIToolkitへ渡すと巨大化する」として固有の固定値
            // (14)を持っていたが、実際の原因はPanelSettingsの既定スケール(ConstantPhysicalSize)に
            // よる高DPI環境での拡大であり、ピクセル等倍(ConstantPixelSize)を明示した時点で
            // 解消していた。同一解像度で3バックエンドの表示が一致することを実機で確認済み(#157)。
            // なお利用者がPanelSettingsを明示指定する場合、そのスケールモードがピクセル等倍で
            // なければ表示サイズはDPIに応じて変わる(UIToolkit側の設定であり、ここでは介入しない).
            var fontSize = theme.FontSize;

            if (_windowRoot.Root != null) _windowRoot.Root.style.backgroundColor = theme.BackgroundColor;

            // ScrollViewの内部クリッピングが、下に配置された兄弟要素(入力欄の行)における
            // 親(Root)自身の背景描画を阻害する現象を確認した(#122調査。resolvedStyle上は
            // 正しい値なのに実描画だけ欠落する。ScrollViewを隠すと直る再現性から、原因は
            // ScrollView側にあると判断)。親の描画に依存せず自己完結するよう、入力欄の行
            // 自体にも同じ背景色を明示的に持たせることで回避する.
            if (_windowRoot.InputRow != null) _windowRoot.InputRow.style.backgroundColor = theme.BackgroundColor;
            ApplyTextElementStyle(_windowRoot.PromptLabel, theme.PromptColor, fontDefinition, fontSize);
            ApplyInputFieldColors(theme);
            ApplyTextElementStyle(_windowRoot.InputField, null, fontDefinition, fontSize);
            ApplyTextElementStyle(_windowRoot.SubmitButton, theme.ExecuteButtonColor, fontDefinition, fontSize);
            ApplyTextElementStyle(_windowRoot.LauncherOpenButton, theme.ButtonColor, fontDefinition, fontSize);
            ApplyTextElementStyle(_windowRoot.LauncherCloseButton, theme.ButtonColor, fontDefinition, fontSize);

            if (LogRenderer != null)
            {
                LogRenderer.CopyButtonColor = theme.CopyButtonColor;
                LogRenderer.FontDefinition = fontDefinition;
                LogRenderer.FontSize = fontSize;
            }

            ApplyScrollViewOptions(scrollSensitivity, scrollDecelerationRate);
        }

        /// <summary>
        /// ログビューの<see cref="ScrollView"/>へ、このバックエンド固有の設定を反映する.
        /// </summary>
        private void ApplyScrollViewOptions(float scrollSensitivity, float scrollDecelerationRate)
        {
            // _windowRootはMonoBehaviourのため、破棄済みを検出できる == null で判定する(?.は素通りする).
            if (_windowRoot == null || _windowRoot.LogScrollView == null) return;

            _windowRoot.LogScrollView.mouseWheelScrollSize = scrollSensitivity;
            _windowRoot.LogScrollView.scrollDecelerationRate = scrollDecelerationRate;

            // ScrollViewのtouchScrollBehavior/scrollDecelerationRateはポインタドラッグ
            // (PointerDown/Move/Up)経由の慣性・弾性(バウンス)専用で、マウスホイール入力
            // (WheelEvent)の処理には一切関与しない(#122調査、Opus協力の上で確認)。
            // ログビューはタッチ操作でのドラッグスクロールもバウンス不要なため、慣性なしの
            // 単純クランプに設定しておく(ホイール由来の「末尾まで届かない」不具合自体の
            // 対策ではない。そちらの実際の原因は UIToolkitCoordinator の自動追従書き戻し漏れと
            // LogRenderer側の毎フレーム再描画によるレイアウトのばたつきだった).
            _windowRoot.LogScrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
        }

        /// <summary>
        /// テーマにFontSizeを設定しても、実際のフォント(<see cref="FontDefinition"/>)が
        /// どのVisualElementにも割り当たっていないと、UIToolkitはグリフの計測ができず
        /// テキストの高さが常に0になる(色・fontSizeは正しく解決されるのに文字が一切
        /// 表示されない不具合として#122で判明)。<see cref="ITerminalTheme.Font"/>未指定時は
        /// プロジェクトのResourcesに頼らず、Unity組み込みの<c>LegacyRuntime.ttf</c>へ
        /// フォールバックする(Arial.ttfはUnity 2022.2で組み込みフォントから外れており、
        /// 本パッケージのUnity要件は6000.0のため追加のフォールバックは不要).
        /// </summary>
        private static FontDefinition ResolveFontDefinition(ITerminalTheme theme)
        {
            var font = theme.Font != null ? theme.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return FontDefinition.FromFont(font);
        }

        private static void ApplyTextElementStyle(VisualElement element, Color? color, FontDefinition fontDefinition, int fontSize)
        {
            if (element == null) return;

            if (color.HasValue) element.style.color = color.Value;
            element.style.unityFontDefinition = fontDefinition;
            element.style.fontSize = fontSize;
        }

        /// <summary>
        /// <see cref="TextField"/>は既定テーマ(unity-theme://default)の標準スキンにより
        /// 白背景の入力ボックスとして描画される。文字色だけでなく、外側の<see cref="TextField"/>と
        /// 内側の<c>unity-text-input</c>(<see cref="TextField.TextInput"/>)双方の背景・枠線も
        /// テーマ色で塗りつぶし、IMGUI版と印象を揃える.
        /// </summary>
        private void ApplyInputFieldColors(ITerminalTheme theme)
        {
            var field = _windowRoot.InputField;
            if (field == null) return;

            field.style.color = theme.InputColor;

            ApplyFieldBoxColors(field, theme.BackgroundColor);
            var textInput = field.Q(TextField.textInputUssName);
            if (textInput != null) ApplyFieldBoxColors(textInput, theme.BackgroundColor);
        }

        private static void ApplyFieldBoxColors(VisualElement element, Color backgroundColor)
        {
            element.style.backgroundColor = backgroundColor;
            element.style.borderTopColor = backgroundColor;
            element.style.borderBottomColor = backgroundColor;
            element.style.borderLeftColor = backgroundColor;
            element.style.borderRightColor = backgroundColor;
        }
    }
}
#endif

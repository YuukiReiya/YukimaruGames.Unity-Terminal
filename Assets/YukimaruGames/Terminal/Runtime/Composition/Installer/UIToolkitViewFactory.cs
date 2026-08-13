#if TERMINAL_UITOOLKIT_AVAILABLE
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.UIToolkit;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// UIToolkitバックエンドの土台(<see cref="UIDocument"/>を載せたGameObjectと
    /// <see cref="WindowRoot"/>)を生成する.
    /// </summary>
    /// <remarks>
    /// UXML/USS/PanelSettingsが未指定のときのフォールバック(警告ログ+実行時生成)もここに閉じる。
    /// 生成に徹し、生成物の寿命は持たない。生成したGameObjectは<see cref="WindowRoot"/>が、
    /// 実行時生成した<see cref="PanelSettings"/>は<see cref="RuntimeGeneratedAsset"/>が
    /// それぞれ所有し、いずれも<see cref="TerminalRuntimeScope"/>の破棄で解放される(#137).
    /// </remarks>
    internal static class UIToolkitViewFactory
    {
        private const string RootGameObjectName = "Terminal UIToolkit Root";

        /// <summary>
        /// GameObject・<see cref="UIDocument"/>・<see cref="WindowRoot"/>を生成する.
        /// </summary>
        /// <returns>
        /// 生成した<see cref="WindowRoot"/>と、<see cref="PanelSettings"/>を実行時生成した場合の
        /// 解放ハンドル(Inspectorで明示指定されていた場合は<c>null</c>)。
        /// 呼び出し側はハンドルをScopeのComponentsへ載せること.
        /// </returns>
        /// <remarks>
        /// 生成したGameObjectはここでは破棄しない。<see cref="WindowRoot"/>(このGameObjectに
        /// アタッチされるMonoBehaviour)が<see cref="System.IDisposable"/>として破棄され、その実装
        /// (WindowRoot.csのIDisposable.Dispose)がDestroy(gameObject)を行うため、結果的にこの
        /// GameObjectも解放される.
        /// </remarks>
        internal static (WindowRoot windowRoot, RuntimeGeneratedAsset generatedPanelSettings) Create(
            VisualTreeAsset visualTreeAsset,
            StyleSheet styleSheet,
            PanelSettings panelSettings)
        {
            var (resolvedPanelSettings, generatedPanelSettings) = ResolvePanelSettings(panelSettings);

            if (visualTreeAsset == null)
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] No VisualTreeAsset assigned for the UIToolkit backend. " +
                    "Falling back to a minimal code-only UI.");
            }

            var rootGameObject = new GameObject(RootGameObjectName);
            var document = rootGameObject.AddComponent<UIDocument>();
            document.visualTreeAsset = visualTreeAsset;
            document.panelSettings = resolvedPanelSettings;

            var windowRoot = rootGameObject.AddComponent<WindowRoot>();
            windowRoot.Initialize(document);

            if (styleSheet != null && windowRoot.Root != null)
            {
                windowRoot.Root.styleSheets.Add(styleSheet);
            }

            return (windowRoot, generatedPanelSettings);
        }

        /// <summary>
        /// <see cref="PanelSettings"/>をInspectorの明示指定から解決する.
        /// </summary>
        /// <remarks>
        /// <c>Resources.Load</c>によるフォールバックは行わない。UIToolkitバックエンドのコード
        /// (Sample「UI Backend: UIToolkit」)とデフォルトアセット(Sample「UI Backend: UIToolkit
        /// Default Resources」)は別々に任意インポートされるため、Resources経由のフォールバックは
        /// 後者を未インポートのままだと機能しない暗黙依存になってしまう(#122で判明)。
        /// 未指定の場合は例外にせず、警告ログのうえ実行時生成する(#129 item1の「例外にせず警告+
        /// フォールバック」方針はResources非依存の形で維持する。UXML未指定時のフォールバックUIは
        /// <see cref="WindowRoot.Initialize"/>側でコードのみで構築される).
        /// </remarks>
        private static (PanelSettings resolved, RuntimeGeneratedAsset generated) ResolvePanelSettings(PanelSettings panelSettings)
        {
            if (panelSettings != null)
            {
                return (panelSettings, null);
            }

            Debug.LogWarning(
                "[YukimaruGames.Terminal] No PanelSettings assigned for the UIToolkit backend. " +
                "Falling back to a runtime-generated PanelSettings.");

            var generated = ScriptableObject.CreateInstance<PanelSettings>();

            // 既定値はConstantPhysicalSize(参照DPIに対する実画面DPIの比率で拡大縮小される)。
            // IMGUI版はDPIスケーリングを一切行わないため、テーマのFontSize等をそのまま
            // 共有すると環境のDPIによって表示サイズが大きく食い違う(#122で判明。Retina等の
            // 高DPI環境でテキストが異常に巨大化する形で顕在化した)。IMGUIと同じ「1px=1px」の
            // 挙動に揃えるため、ピクセル等倍を明示する.
            generated.scaleMode = PanelScaleMode.ConstantPixelSize;

            return (generated, new RuntimeGeneratedAsset(generated));
        }
    }
}
#endif

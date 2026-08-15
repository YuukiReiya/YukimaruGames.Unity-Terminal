#if TERMINAL_UGUI_AVAILABLE
using UnityEngine;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.UGUI;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// uGUIバックエンドの土台(<see cref="Canvas"/>・<see cref="WindowRoot"/>)を生成する.
    /// </summary>
    /// <remarks>
    /// 生成に徹し、生成物の寿命は持たない。GameObjectは<see cref="WindowRoot"/>が所有し、
    /// <see cref="TerminalRuntimeScope"/>の破棄で解放される
    /// (UIToolkit版の<c>UIToolkitViewFactory</c>と同じ方針)。
    /// <para>
    /// <c>EventSystem</c>の用意はここでは行わない。<c>Awake</c>の時点では
    /// シーン上の<c>EventSystem</c>が未登録で見落としうるため、
    /// <see cref="EventSystemProvisioner"/>が全<c>OnEnable</c>完了後に解決する(#152).
    /// </para>
    /// </remarks>
    internal static class UGUIViewFactory
    {
        private const string CanvasGameObjectName = "Terminal UGUI Root";

        /// <summary>
        /// Canvasと<see cref="WindowRoot"/>を生成する.
        /// </summary>
        /// <param name="prefab">
        /// Canvas配下のUIツリーを持つPrefab。<c>null</c>の場合はコードのみで最小構成を組み立てる.
        /// </param>
        /// <param name="sortingOrder">Canvasの描画順.</param>
        /// <param name="scaleFactor">UI全体の拡大率(1でIMGUI版と同じ「1px = 1px」).</param>
        /// <returns>生成した<see cref="WindowRoot"/>.</returns>
        internal static WindowRoot Create(
            GameObject prefab,
            int sortingOrder,
            float scaleFactor)
        {
            GameObject canvasGameObject = null;

            try
            {
                canvasGameObject = CreateCanvas(prefab, sortingOrder, scaleFactor, out var canvasRoot);

                var windowRoot = canvasGameObject.AddComponent<WindowRoot>();
                windowRoot.Initialize(canvasRoot);

                return windowRoot;
            }
            catch
            {
                // 生成途中で失敗した場合、まだ誰にも所有権を渡せていないためここで後始末する
                // (平常時の所有はTerminalRuntimeScope側に残す方針は変えない).
                if (canvasGameObject != null)
                {
                    DestroyObject(canvasGameObject);
                }

                throw;
            }
        }

        private static GameObject CreateCanvas(GameObject prefab, int sortingOrder, float scaleFactor, out RectTransform canvasRoot)
        {
            GameObject canvasGameObject;

            if (prefab != null)
            {
                canvasGameObject = Object.Instantiate(prefab);
                canvasGameObject.name = CanvasGameObjectName;
            }
            else
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] No prefab assigned for the uGUI backend. " +
                    "Falling back to a minimal code-only UI.");
                canvasGameObject = new GameObject(CanvasGameObjectName);
            }

            var canvas = GetOrAddComponent<Canvas>(canvasGameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            // IMGUI版の「1px = 1px」に揃える。UIToolkit版はPanelSettingsの既定
            // (ConstantPhysicalSize)が高DPI環境で表示サイズを大きく狂わせた(#122).
            var scaler = GetOrAddComponent<CanvasScaler>(canvasGameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scaleFactor > 0f ? scaleFactor : 1f;

            GetOrAddComponent<GraphicRaycaster>(canvasGameObject);

            canvasRoot = canvasGameObject.GetComponent<RectTransform>();
            return canvasGameObject;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void DestroyObject(Object target)
        {
            if (UnityEngine.Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
#endif

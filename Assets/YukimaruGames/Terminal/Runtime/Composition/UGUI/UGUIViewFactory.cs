#if TERMINAL_UGUI_AVAILABLE
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.UGUI;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// uGUIバックエンドの土台(<see cref="Canvas"/>・<see cref="EventSystem"/>・
    /// <see cref="WindowRoot"/>)を生成する.
    /// </summary>
    /// <remarks>
    /// 生成に徹し、生成物の寿命は持たない。GameObjectは<see cref="WindowRoot"/>が、
    /// 自前生成した<see cref="EventSystem"/>は<see cref="RuntimeGeneratedAsset"/>が所有し、
    /// いずれも<see cref="TerminalRuntimeScope"/>の破棄で解放される
    /// (UIToolkit版の<c>UIToolkitViewFactory</c>と同じ方針).
    /// </remarks>
    internal static class UGUIViewFactory
    {
        private const string CanvasGameObjectName = "Terminal UGUI Root";
        private const string EventSystemGameObjectName = "Terminal UGUI EventSystem";

        /// <summary>
        /// Canvas・WindowRoot・(必要なら)EventSystemを生成する.
        /// </summary>
        /// <param name="prefab">
        /// Canvas配下のUIツリーを持つPrefab。<c>null</c>の場合はコードのみで最小構成を組み立てる.
        /// </param>
        /// <param name="sortingOrder">Canvasの描画順.</param>
        /// <param name="scaleFactor">UI全体の拡大率(1でIMGUI版と同じ「1px = 1px」).</param>
        /// <param name="useInputSystemModule">
        /// EventSystemを自前生成する場合に、Input System用の入力モジュールを使うか.
        /// </param>
        /// <returns>
        /// 生成した<see cref="WindowRoot"/>と、EventSystemを自前生成した場合の解放ハンドル
        /// (既存のEventSystemがあった場合は<c>null</c>)。
        /// 呼び出し側はハンドルをScopeのComponentsへ載せること.
        /// </returns>
        internal static (WindowRoot windowRoot, RuntimeGeneratedAsset generatedEventSystem) Create(
            GameObject prefab,
            int sortingOrder,
            float scaleFactor,
            bool useInputSystemModule)
        {
            var generatedEventSystem = EnsureEventSystem(useInputSystemModule);

            GameObject canvasGameObject = null;

            try
            {
                canvasGameObject = CreateCanvas(prefab, sortingOrder, scaleFactor, out var canvasRoot);

                var windowRoot = canvasGameObject.AddComponent<WindowRoot>();
                windowRoot.Initialize(canvasRoot);

                return (windowRoot, generatedEventSystem);
            }
            catch
            {
                // 生成途中で失敗した場合、まだ誰にも所有権を渡せていないためここで後始末する
                // (平常時の所有はTerminalRuntimeScope側に残す方針は変えない).
                if (canvasGameObject != null)
                {
                    DestroyObject(canvasGameObject);
                }

                generatedEventSystem?.Dispose();
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

        /// <summary>
        /// <see cref="EventSystem"/>が無い場合のみ生成する.
        /// </summary>
        /// <remarks>
        /// uGUIはEventSystemが無いとボタンのクリックも<see cref="InputField"/>のフォーカスも
        /// 一切機能しない。既存があれば触らない(破棄も自前生成分のみ)。
        /// 判定は<c>FindObjectOfType</c>ではなくstaticな<see cref="EventSystem.current"/>で行う
        /// (プロジェクト規約で前者は禁止).
        /// </remarks>
        private static RuntimeGeneratedAsset EnsureEventSystem(bool useInputSystemModule)
        {
            if (EventSystem.current != null) return null;

            var eventSystemGameObject = new GameObject(EventSystemGameObjectName, typeof(EventSystem));

            // Active Input HandlingがInput System専用の環境でStandaloneInputModuleを使うと
            // 実行時例外になるため、解決済みの入力方式に合わせて選び分ける.
#if ENABLE_INPUT_SYSTEM
            if (useInputSystemModule)
            {
                eventSystemGameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                AddLegacyInputModule(eventSystemGameObject);
            }
#else
            AddLegacyInputModule(eventSystemGameObject);
#endif

            return new RuntimeGeneratedAsset(eventSystemGameObject);
        }

        /// <summary>
        /// レガシー入力用のモジュールを付ける.
        /// </summary>
        /// <remarks>
        /// Active Input HandlingがInput System専用の環境では<c>StandaloneInputModule</c>が
        /// 実行時例外になるため付けられない。その場合はInput System用のモジュールへ
        /// フォールバックする。ここで何も付けずに抜けると、EventSystemに入力モジュールが
        /// 存在しない状態になり、ボタンのクリックも<see cref="InputField"/>のフォーカスも
        /// 一切機能しないまま警告も出ず、原因の特定が難しくなる.
        /// </remarks>
        private static void AddLegacyInputModule(GameObject eventSystemGameObject)
        {
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            eventSystemGameObject.AddComponent<StandaloneInputModule>();
#else
            eventSystemGameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
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

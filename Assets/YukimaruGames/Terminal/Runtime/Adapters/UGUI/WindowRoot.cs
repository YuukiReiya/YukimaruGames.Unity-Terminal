#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UI;
using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Adapters.UGUI
{
    /// <summary>
    /// uGUI(<see cref="Canvas"/>配下)のターミナルUIツリーを保持し、各要素への参照を提供する.
    /// </summary>
    /// <remarks>
    /// Prefabが指定された場合は、その配下の要素を<b>名前で解決</b>する
    /// (UIToolkit版がUXMLを<c>Q&lt;T&gt;(name)</c>で解決しているのと同じ方式)。
    /// デフォルトPrefabとコードは別々のSampleとして独立にImportされるため、Prefab側へ
    /// カスタムスクリプトをアタッチしてしまうと、Resources Sampleだけを入れた環境で
    /// Missing Scriptになる。そのためPrefabには素のuGUIコンポーネントのみを置き、
    /// 本クラスは実行時に<c>AddComponent</c>する(#139)。
    ///
    /// Prefab未指定時は<see cref="BuildMinimalTree"/>でコードのみの最小構成を組み立てる。
    /// <c>Resources.Load</c>によるフォールバックは行わない(#122で確定した方針).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WindowRoot : MonoBehaviour, IDisposable
    {
        #region element-names

        /// <summary>ウィンドウ本体.</summary>
        public const string RootName = "terminal-root";
        /// <summary>入力欄の行(プロンプト・入力欄・実行ボタンを含む).</summary>
        public const string InputRowName = "terminal-input-row";
        /// <summary>ログ表示のScrollRect.</summary>
        public const string LogScrollViewName = "log-scroll-view";
        /// <summary>ログ行が並ぶContent.</summary>
        public const string LogContentName = "log-content";
        /// <summary>コマンド入力欄.</summary>
        public const string InputFieldName = "input-field";
        /// <summary>実行ボタン.</summary>
        public const string SubmitButtonName = "submit-button";
        /// <summary>プロンプト表示.</summary>
        public const string PromptLabelName = "prompt-label";
        /// <summary>ランチャーボタンの入れ物.</summary>
        public const string LauncherContainerName = "launcher-container";
        /// <summary>ウィンドウを開くボタン.</summary>
        public const string LauncherOpenButtonName = "launcher-open-button";
        /// <summary>ウィンドウを閉じるボタン.</summary>
        public const string LauncherCloseButtonName = "launcher-close-button";

        #endregion

        /// <summary>ウィンドウ本体の<see cref="RectTransform"/>.</summary>
        public RectTransform Root { get; private set; }

        /// <summary>ウィンドウ本体の背景.</summary>
        public Image RootBackground { get; private set; }

        /// <summary>入力欄の行.</summary>
        public RectTransform InputRow { get; private set; }

        /// <summary>入力欄の行の背景.</summary>
        public Image InputRowBackground { get; private set; }

        /// <summary>ログ表示のScrollRect.</summary>
        public ScrollRect LogScrollView { get; private set; }

        /// <summary>ログ行が並ぶContent.</summary>
        public RectTransform LogContent { get; private set; }

        /// <summary>コマンド入力欄.</summary>
        public InputField InputField { get; private set; }

        /// <summary>実行ボタン.</summary>
        public Button SubmitButton { get; private set; }

        /// <summary>プロンプト表示.</summary>
        public Text PromptLabel { get; private set; }

        /// <summary>ランチャーボタンの入れ物.</summary>
        public RectTransform LauncherContainer { get; private set; }

        /// <summary>ウィンドウを開くボタン.</summary>
        public Button LauncherOpenButton { get; private set; }

        /// <summary>ウィンドウを閉じるボタン.</summary>
        public Button LauncherCloseButton { get; private set; }

        /// <summary>
        /// 各要素の解決に成功しているか.
        /// </summary>
        /// <remarks>
        /// 解決に失敗した状態でRendererが描画を試みると<c>NullReferenceException</c>が多発するため、
        /// 利用側はこのフラグで早期リターンする.
        /// </remarks>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// UIツリーを解決する.
        /// </summary>
        /// <param name="canvasRoot">
        /// Prefabから生成したCanvas配下のルート。<c>null</c>の場合はコードのみで最小構成を組み立てる.
        /// </param>
        public void Initialize(RectTransform canvasRoot)
        {
            IsInitialized = false;

            if (canvasRoot == null)
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] No canvas root provided for the uGUI backend. " +
                    "Falling back to a minimal code-only UI.");
                return;
            }

            Root = ResolveRoot(canvasRoot);
            if (Root == null)
            {
                Root = BuildMinimalTree(canvasRoot);
            }
            else
            {
                ResolveChildren();
            }

            IsInitialized =
                Root != null &&
                InputRow != null &&
                LogScrollView != null &&
                LogContent != null &&
                InputField != null &&
                SubmitButton != null &&
                PromptLabel != null &&
                LauncherContainer != null &&
                LauncherOpenButton != null &&
                LauncherCloseButton != null;

            if (!IsInitialized)
            {
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] Failed to resolve one or more uGUI elements by name. " +
                    "The terminal window will not be rendered.");
            }
        }

        /// <summary>
        /// <see cref="TerminalRect"/>(左上原点・Y下向き)をウィンドウ本体へ反映する.
        /// </summary>
        /// <remarks>
        /// <see cref="RectTransform"/>はY上向きのため、アンカーとpivotを左上(0,1)に固定したうえで
        /// <c>anchoredPosition.y</c>の符号を反転させる.
        /// </remarks>
        public void ApplyRect(TerminalRect rect)
        {
            if (Root == null) return;

            Root.anchorMin = new Vector2(0f, 1f);
            Root.anchorMax = new Vector2(0f, 1f);
            Root.pivot = new Vector2(0f, 1f);
            Root.anchoredPosition = new Vector2(rect.X, -rect.Y);
            Root.sizeDelta = new Vector2(rect.Width, rect.Height);
        }

        private RectTransform ResolveRoot(RectTransform canvasRoot)
        {
            if (canvasRoot.name == RootName) return canvasRoot;

            var found = canvasRoot.Find(RootName) as RectTransform;
            return found;
        }

        private void ResolveChildren()
        {
            RootBackground = Root.GetComponent<Image>();

            InputRow = FindChild<RectTransform>(InputRowName);
            InputRowBackground = InputRow != null ? InputRow.GetComponent<Image>() : null;

            LogScrollView = FindChild<ScrollRect>(LogScrollViewName);
            LogContent = FindChild<RectTransform>(LogContentName);
            InputField = FindChild<InputField>(InputFieldName);
            SubmitButton = FindChild<Button>(SubmitButtonName);
            PromptLabel = FindChild<Text>(PromptLabelName);
            LauncherContainer = FindChild<RectTransform>(LauncherContainerName);
            LauncherOpenButton = FindChild<Button>(LauncherOpenButtonName);
            LauncherCloseButton = FindChild<Button>(LauncherCloseButtonName);
        }

        /// <summary>
        /// 名前で子孫を探索する.
        /// </summary>
        /// <remarks>
        /// <c>GameObject.Find</c>はプロジェクト規約で禁止のため使わない。
        /// <see cref="Component.GetComponentsInChildren{T}(bool)"/>で自分の配下だけを走査する
        /// (非アクティブな要素も対象にするため<c>includeInactive: true</c>).
        /// </remarks>
        private T FindChild<T>(string childName) where T : Component
        {
            var candidates = Root.GetComponentsInChildren<T>(true);
            foreach (var candidate in candidates)
            {
                if (candidate.name == childName) return candidate;
            }

            return null;
        }

        private RectTransform BuildMinimalTree(RectTransform parent)
        {
            var root = CreateElement(RootName, parent);
            RootBackground = root.gameObject.AddComponent<Image>();

            var scrollView = CreateElement(LogScrollViewName, root);
            LogScrollView = scrollView.gameObject.AddComponent<ScrollRect>();
            LogScrollView.horizontal = false;
            LogScrollView.vertical = true;
            LogScrollView.movementType = ScrollRect.MovementType.Clamped;
            scrollView.gameObject.AddComponent<RectMask2D>();

            LogContent = CreateElement(LogContentName, scrollView);
            LogContent.anchorMin = new Vector2(0f, 1f);
            LogContent.anchorMax = new Vector2(1f, 1f);
            LogContent.pivot = new Vector2(0f, 1f);
            var contentLayout = LogContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            var contentFitter = LogContent.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            LogScrollView.content = LogContent;
            LogScrollView.viewport = scrollView;

            InputRow = CreateElement(InputRowName, root);
            InputRowBackground = InputRow.gameObject.AddComponent<Image>();
            var inputRowLayout = InputRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.childControlHeight = true;
            inputRowLayout.childControlWidth = true;
            inputRowLayout.childForceExpandWidth = false;

            PromptLabel = CreateText(PromptLabelName, InputRow);

            var inputFieldElement = CreateElement(InputFieldName, InputRow);
            var inputFieldImage = inputFieldElement.gameObject.AddComponent<Image>();
            InputField = inputFieldElement.gameObject.AddComponent<InputField>();
            InputField.targetGraphic = inputFieldImage;
            InputField.textComponent = CreateText($"{InputFieldName}-text", inputFieldElement);
            InputField.lineType = InputField.LineType.SingleLine;

            var submitElement = CreateElement(SubmitButtonName, InputRow);
            var submitImage = submitElement.gameObject.AddComponent<Image>();
            SubmitButton = submitElement.gameObject.AddComponent<Button>();
            SubmitButton.targetGraphic = submitImage;
            CreateText($"{SubmitButtonName}-text", submitElement);

            LauncherContainer = CreateElement(LauncherContainerName, parent);
            LauncherOpenButton = CreateLauncherButton(LauncherOpenButtonName, LauncherContainer);
            LauncherCloseButton = CreateLauncherButton(LauncherCloseButtonName, LauncherContainer);

            return root;
        }

        private static RectTransform CreateElement(string elementName, RectTransform parent)
        {
            var element = new GameObject(elementName, typeof(RectTransform));
            var rectTransform = (RectTransform)element.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static Text CreateText(string elementName, RectTransform parent)
        {
            var element = CreateElement(elementName, parent);
            var text = element.gameObject.AddComponent<Text>();
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateLauncherButton(string elementName, RectTransform parent)
        {
            var element = CreateElement(elementName, parent);
            var image = element.gameObject.AddComponent<Image>();
            var button = element.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText($"{elementName}-text", element);
            return button;
        }

        /// <summary>
        /// 動的生成された自身の<see cref="GameObject"/>を破棄する.
        /// </summary>
        /// <remarks>
        /// <see cref="Composition"/>層への逆依存を避けるため、破棄の分岐はここで直接実装する
        /// (UIToolkit版の<c>WindowRoot</c>と同じ理由).
        /// </remarks>
        void IDisposable.Dispose()
        {
            if (this == null || gameObject == null) return;

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
#endif

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

        /// <summary>コード生成フォールバック時の内側余白(px).</summary>
        private const int ContentPadding = 4;
        /// <summary>コード生成フォールバック時の入力行の高さ(px).</summary>
        private const float InputRowHeight = 64f;
        /// <summary>コード生成フォールバック時のプロンプト表示幅(px).</summary>
        private const float PromptWidth = 48f;
        /// <summary>コード生成フォールバック時の実行ボタン幅(px).</summary>
        private const float SubmitButtonWidth = 160f;
        /// <summary>コード生成フォールバック時のマウスホイール感度.</summary>
        private const float DefaultScrollSensitivity = 24f;
        /// <summary>コード生成フォールバック時のランチャーボタンの一辺(px).</summary>
        private const float LauncherButtonSize = 64f;

        /// <summary>
        /// 名前解決の起点(Canvas配下のルート).
        /// </summary>
        /// <remarks>
        /// ランチャーはウィンドウ本体の<b>外側</b>に配置されるため<c>terminal-root</c>の兄弟として
        /// 存在する。<c>terminal-root</c>配下だけを探すと解決できないので、起点はCanvas側に取る.
        /// </remarks>
        private RectTransform _canvasRoot;

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
        /// Prefabから生成したCanvas配下のルート。名前解決の起点であり、<c>null</c>は許容しない
        /// (最小構成の組み立てにも親となる<see cref="RectTransform"/>が要るため)。
        /// <c>null</c>を渡した場合は警告を出し、<see cref="IsInitialized"/>を<c>false</c>のまま復帰する。
        /// コード生成の最小構成へ切り替わるのは、<c>canvasRoot</c>配下から<c>terminal-root</c>を
        /// 解決できなかった場合(Prefab未指定でCanvasだけ生成された場合など).
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

            _canvasRoot = canvasRoot;

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
        /// <see cref="Component.GetComponentsInChildren{T}(bool)"/>で<see cref="_canvasRoot"/>の
        /// 配下だけを走査する(非アクティブな要素も対象にするため<c>includeInactive: true</c>).
        /// </remarks>
        private T FindChild<T>(string childName) where T : Component
        {
            var origin = _canvasRoot != null ? _canvasRoot : Root;
            var candidates = origin.GetComponentsInChildren<T>(true);
            foreach (var candidate in candidates)
            {
                if (candidate.name == childName) return candidate;
            }

            return null;
        }

        /// <summary>
        /// Prefab未指定時に、コードのみで最小構成のUIツリーを組み立てる.
        /// </summary>
        /// <remarks>
        /// レイアウトはLayoutGroupに委ねる。ウィンドウ本体のサイズは<see cref="ApplyRect"/>が
        /// 毎フレーム反映するため、ここでは子要素の比率(ログが可変・入力行が固定高さ)だけを決める.
        /// </remarks>
        private RectTransform BuildMinimalTree(RectTransform parent)
        {
            var root = CreateElement(RootName, parent);
            RootBackground = root.gameObject.AddComponent<Image>();
            var rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.padding = new RectOffset(ContentPadding, ContentPadding, ContentPadding, ContentPadding);

            var scrollView = CreateElement(LogScrollViewName, root);
            var scrollViewLayout = scrollView.gameObject.AddComponent<LayoutElement>();
            scrollViewLayout.flexibleHeight = 1f;
            LogScrollView = scrollView.gameObject.AddComponent<ScrollRect>();
            LogScrollView.horizontal = false;
            LogScrollView.vertical = true;
            LogScrollView.movementType = ScrollRect.MovementType.Clamped;
            LogScrollView.scrollSensitivity = DefaultScrollSensitivity;
            scrollView.gameObject.AddComponent<RectMask2D>();

            LogContent = CreateElement(LogContentName, scrollView);
            LogContent.anchorMin = new Vector2(0f, 1f);
            LogContent.anchorMax = new Vector2(1f, 1f);
            LogContent.pivot = new Vector2(0f, 1f);
            LogContent.offsetMin = new Vector2(0f, LogContent.offsetMin.y);
            LogContent.offsetMax = new Vector2(0f, LogContent.offsetMax.y);
            var contentLayout = LogContent.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = LogContent.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // viewportはScrollRect自身(RectMask2Dでクリップする)を使う.
            LogScrollView.viewport = scrollView;
            LogScrollView.content = LogContent;

            InputRow = CreateElement(InputRowName, root);
            InputRowBackground = InputRow.gameObject.AddComponent<Image>();
            var inputRowLayoutElement = InputRow.gameObject.AddComponent<LayoutElement>();
            inputRowLayoutElement.minHeight = InputRowHeight;
            inputRowLayoutElement.preferredHeight = InputRowHeight;
            inputRowLayoutElement.flexibleHeight = 0f;
            var inputRowLayout = InputRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            inputRowLayout.childControlHeight = true;
            inputRowLayout.childControlWidth = true;
            inputRowLayout.childForceExpandHeight = true;
            inputRowLayout.childForceExpandWidth = false;
            inputRowLayout.childAlignment = TextAnchor.MiddleLeft;

            PromptLabel = CreateText(PromptLabelName, InputRow);
            PromptLabel.alignment = TextAnchor.MiddleLeft;
            var promptLayout = PromptLabel.gameObject.AddComponent<LayoutElement>();
            promptLayout.preferredWidth = PromptWidth;
            promptLayout.flexibleWidth = 0f;

            var inputFieldElement = CreateElement(InputFieldName, InputRow);
            var inputFieldLayout = inputFieldElement.gameObject.AddComponent<LayoutElement>();
            inputFieldLayout.flexibleWidth = 1f;
            var inputFieldImage = inputFieldElement.gameObject.AddComponent<Image>();
            InputField = inputFieldElement.gameObject.AddComponent<InputField>();
            InputField.targetGraphic = inputFieldImage;

            var inputText = CreateText($"{InputFieldName}-text", inputFieldElement);
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.rectTransform.anchorMin = Vector2.zero;
            inputText.rectTransform.anchorMax = Vector2.one;
            inputText.rectTransform.offsetMin = Vector2.zero;
            inputText.rectTransform.offsetMax = Vector2.zero;
            InputField.textComponent = inputText;
            InputField.lineType = InputField.LineType.SingleLine;

            var submitElement = CreateElement(SubmitButtonName, InputRow);
            var submitLayout = submitElement.gameObject.AddComponent<LayoutElement>();
            submitLayout.preferredWidth = SubmitButtonWidth;
            submitLayout.flexibleWidth = 0f;
            var submitImage = submitElement.gameObject.AddComponent<Image>();
            SubmitButton = submitElement.gameObject.AddComponent<Button>();
            SubmitButton.targetGraphic = submitImage;
            var submitText = CreateText($"{SubmitButtonName}-text", submitElement);
            submitText.alignment = TextAnchor.MiddleCenter;
            submitText.rectTransform.anchorMin = Vector2.zero;
            submitText.rectTransform.anchorMax = Vector2.one;
            submitText.rectTransform.offsetMin = Vector2.zero;
            submitText.rectTransform.offsetMax = Vector2.zero;

            // uGUIのHorizontalLayoutGroupとVerticalLayoutGroupは同一GameObjectに共存できない
            // (どちらもHorizontalOrVerticalLayoutGroupで、Unityが後勝ちの追加を拒否する)。
            // ランチャーは要素が2つだけなので、LayoutGroupに頼らずLauncherRenderer側が
            // アンカーに応じて直接配置する.
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

            element.anchorMin = new Vector2(0f, 1f);
            element.anchorMax = new Vector2(0f, 1f);
            element.pivot = new Vector2(0f, 1f);
            element.sizeDelta = new Vector2(LauncherButtonSize, LauncherButtonSize);

            var label = CreateText($"{elementName}-text", element);
            label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
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

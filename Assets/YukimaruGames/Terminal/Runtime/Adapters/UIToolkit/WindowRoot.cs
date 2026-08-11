#if TERMINAL_UITOOLKIT_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Adapters.UIToolkit
{
    /// <summary>
    /// <see cref="UIDocument"/>のUXML階層からターミナルの各構成要素を解決し、
    /// ウィンドウ全体のRectを反映するMonoBehaviour.
    /// </summary>
    /// <remarks>
    /// 契約(Presentationのインターフェース)は持たない。<see cref="UIDocument"/>を保持できるのが
    /// MonoBehaviourのみであるため、Presentation層ではなくAdapters層(UIToolkitInstallerが動的生成する
    /// GameObjectへアタッチ)に配置する(既存<c>Adapters/IMGUI/TerminalView.cs</c>と同様の位置付け).
    /// </remarks>
    public sealed class WindowRoot : MonoBehaviour, IDisposable
    {
        public const string RootName = "terminal-root";
        public const string InputRowName = "terminal-input-row";
        public const string LogScrollViewName = "log-scroll-view";
        public const string InputFieldName = "input-field";
        public const string SubmitButtonName = "submit-button";
        public const string PromptLabelName = "prompt-label";
        public const string LauncherContainerName = "launcher-container";
        public const string LauncherOpenButtonName = "launcher-open-button";
        public const string LauncherCloseButtonName = "launcher-close-button";

        private UIDocument _document;

        public VisualElement Root { get; private set; }
        public VisualElement InputRow { get; private set; }
        public ScrollView LogScrollView { get; private set; }
        public TextField InputField { get; private set; }
        public Button SubmitButton { get; private set; }
        public Label PromptLabel { get; private set; }
        public VisualElement LauncherContainer { get; private set; }
        public Button LauncherOpenButton { get; private set; }
        public Button LauncherCloseButton { get; private set; }

        public bool IsInitialized { get; private set; }

        public void Initialize(UIDocument document)
        {
            _document = document;
            var rootVisualElement = _document != null ? _document.rootVisualElement : null;
            if (rootVisualElement == null)
            {
                IsInitialized = false;
                return;
            }

            // VisualTreeAssetが未指定(UIDocument.visualTreeAsset == null)の場合、
            // rootVisualElementに名前付き要素が存在しないため、コードのみで最小限の
            // ツリーを組み立てる(Resources.Loadへの暗黙依存を避けるためのフォールバック).
            if (document.visualTreeAsset == null)
            {
                BuildMinimalTree(rootVisualElement);
            }
            else
            {
                Root = rootVisualElement.Q<VisualElement>(RootName) ?? rootVisualElement;
                InputRow = rootVisualElement.Q<VisualElement>(InputRowName);
                LogScrollView = rootVisualElement.Q<ScrollView>(LogScrollViewName);
                InputField = rootVisualElement.Q<TextField>(InputFieldName);
                SubmitButton = rootVisualElement.Q<Button>(SubmitButtonName);
                PromptLabel = rootVisualElement.Q<Label>(PromptLabelName);
                LauncherContainer = rootVisualElement.Q<VisualElement>(LauncherContainerName);
                LauncherOpenButton = rootVisualElement.Q<Button>(LauncherOpenButtonName);
                LauncherCloseButton = rootVisualElement.Q<Button>(LauncherCloseButtonName);
            }

            Root.style.position = Position.Absolute;

            // ScrollViewの内部ビューポート(unity-content-viewport)のクリップは既定テーマ由来の
            // USSルール(overflow: hidden)に依存しており、テーマ(themeUss)未適用の環境
            // (Resourcesフォールバック時のPanelSettings等)ではクリップが効かず、ログ内容が
            // ウィンドウ枠を越えて描画されてしまう不具合が実機検証で確認された(#122)。
            // テーマの有無に関わらずクリップされるよう明示的に指定する.
            if (LogScrollView != null)
            {
                LogScrollView.style.overflow = Overflow.Hidden;

                var contentViewport = LogScrollView.Q<VisualElement>("unity-content-viewport");
                if (contentViewport != null)
                {
                    contentViewport.style.overflow = Overflow.Hidden;
                }
            }

            IsInitialized = true;
        }

        /// <summary>
        /// このフォールバックはPanelSettingsも未指定(<c>ScriptableObject.CreateInstance</c>による
        /// 代替)である前提で使われるため、テーマ(themeUss)が一切適用されない。<c>font-size</c>等の
        /// 既定スタイルがテーマ由来のためゼロになり、文字が一切見えなくなる(#122で判明)。
        /// テーマの有無に関わらず表示されるよう、フォントサイズを含め必要なスタイルは全て
        /// このメソッド内でインラインに明示指定する.
        /// </summary>
        private const int FallbackFontSize = 14;

        /// <summary>
        /// <c>TerminalWindow.uxml</c>と同じ名前・階層構成を持つ最小限のツリーをコードのみで構築する.
        /// </summary>
        private void BuildMinimalTree(VisualElement parent)
        {
            Root = new VisualElement { name = RootName };
            Root.style.flexDirection = FlexDirection.Column;
            Root.style.backgroundColor = new Color(0f, 0f, 0f, 0.85f);

            LogScrollView = new ScrollView(ScrollViewMode.Vertical) { name = LogScrollViewName };
            LogScrollView.style.flexGrow = 1;
            LogScrollView.style.flexShrink = 1;

            InputRow = new VisualElement { name = InputRowName };
            InputRow.style.flexDirection = FlexDirection.Row;
            InputRow.style.flexShrink = 0;
            InputRow.style.alignItems = Align.Center;
            InputRow.style.paddingLeft = 4;
            InputRow.style.paddingRight = 4;
            InputRow.style.paddingTop = 2;
            InputRow.style.paddingBottom = 2;

            PromptLabel = new Label("$") { name = PromptLabelName };
            PromptLabel.style.marginRight = 4;
            PromptLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            PromptLabel.style.fontSize = FallbackFontSize;

            InputField = new TextField { name = InputFieldName };
            InputField.style.flexGrow = 1;
            InputField.style.fontSize = FallbackFontSize;

            SubmitButton = new Button { text = "| exec", name = SubmitButtonName };
            SubmitButton.style.marginLeft = 4;
            SubmitButton.style.fontSize = FallbackFontSize;

            InputRow.Add(PromptLabel);
            InputRow.Add(InputField);
            InputRow.Add(SubmitButton);

            Root.Add(LogScrollView);
            Root.Add(InputRow);

            LauncherContainer = new VisualElement { name = LauncherContainerName };
            LauncherContainer.style.position = Position.Absolute;
            LauncherContainer.style.flexDirection = FlexDirection.Row;

            LauncherOpenButton = new Button { text = "[-]", name = LauncherOpenButtonName };
            LauncherOpenButton.style.minWidth = 24;
            LauncherOpenButton.style.fontSize = FallbackFontSize;
            LauncherCloseButton = new Button { text = "[x]", name = LauncherCloseButtonName };
            LauncherCloseButton.style.minWidth = 24;
            LauncherCloseButton.style.fontSize = FallbackFontSize;

            LauncherContainer.Add(LauncherOpenButton);
            LauncherContainer.Add(LauncherCloseButton);

            parent.Add(Root);
            parent.Add(LauncherContainer);
        }

        /// <summary>
        /// <see cref="TerminalRect"/>をルート<see cref="VisualElement"/>のstyleへ反映する.
        /// </summary>
        /// <remarks>
        /// <see cref="WindowAnimator"/>はOpen/Closeアニメーション中、アンカーに応じて
        /// <c>X</c>または<c>Y</c>のみを毎フレーム変化させ、<c>Width</c>/<c>Height</c>は
        /// アニメーション中一定に保たれる(スライドイン/アウト方式)。この位置成分を毎フレーム
        /// <c>style.left</c>/<c>style.top</c>(レイアウトプロパティ)で書き換え続けると、UIToolkit
        /// ランタイム側のクリップ矩形キャッシュが古い(閉じていた時の小さい)状態のまま更新されず、
        /// アニメーション完了後も一部領域が描画されなくなる不具合が実機検証で確認された(#122)。
        /// <c>left</c>/<c>top</c>は<c>0</c>に固定し、位置は再レイアウトを伴わない
        /// <c>style.translate</c>(GPU transform)側に逃がすことでこれを回避する.
        /// </remarks>
        public void ApplyRect(TerminalRect rect)
        {
            if (Root == null) return;

            Root.style.left = 0;
            Root.style.top = 0;
            Root.style.width = rect.Width;
            Root.style.height = rect.Height;
            Root.style.translate = new Translate(rect.X, rect.Y);
        }

        /// <summary>
        /// 動的生成された自身の<see cref="GameObject"/>を破棄する.
        /// </summary>
        /// <remarks>
        /// <see cref="Composition.Shared.Extensions.UnityObjectExtensions.Destroy"/>相当の分岐を
        /// Adapters層内で完結させるため、Composition層への逆依存を避けてここで直接実装する.
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

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

        /// <summary>ウィンドウ全体のルート要素.</summary>
        public VisualElement Root { get; private set; }

        /// <summary>入力欄(プロンプト・TextField・実行ボタン)を並べる行.</summary>
        public VisualElement InputRow { get; private set; }

        /// <summary>ログ表示用のスクロールビュー.</summary>
        public ScrollView LogScrollView { get; private set; }

        /// <summary>コマンド入力欄.</summary>
        public TextField InputField { get; private set; }

        /// <summary>コマンド実行ボタン.</summary>
        public Button SubmitButton { get; private set; }

        /// <summary>プロンプト表示用のラベル.</summary>
        public Label PromptLabel { get; private set; }

        /// <summary>ランチャー(開閉)ボタンをまとめるコンテナ.</summary>
        public VisualElement LauncherContainer { get; private set; }

        /// <summary>ウィンドウを開くランチャーボタン.</summary>
        public Button LauncherOpenButton { get; private set; }

        /// <summary>ウィンドウを閉じるランチャーボタン.</summary>
        public Button LauncherCloseButton { get; private set; }

        /// <summary><see cref="Initialize"/>が完了しているか.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 指定した<see cref="UIDocument"/>のルート要素から各構成要素を解決し、初期化する.
        /// </summary>
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
            //
            // 一時的にScrollView自身へのoverflow:Hidden指定がスクロール可能量(highValue)を
            // 過小評価させている疑いを検証したが、除去すると本来のクリップ不具合(枠外へのはみ出し)
            // が再発する一方でスクロール不具合自体は解消しなかったため、この仮説は誤りと判明し
            // 元に戻した(#122).
            if (LogScrollView != null)
            {
                LogScrollView.style.overflow = Overflow.Hidden;

                var contentViewport = LogScrollView.Q<VisualElement>("unity-content-viewport");
                if (contentViewport != null)
                {
                    contentViewport.style.overflow = Overflow.Hidden;
                }

                // ScrollView内部の unity-content-container は、既定テーマUSS側で
                // flex-shrink:0 が指定されている前提の実装になっている。テーマ未適用の環境
                // (Resourcesフォールバック時のPanelSettings等)ではこれが適用されず、CSSの既定値
                // (flex-shrink:1)のまま残るため、ログ行の総高さ(例: 203px)がビューポートの高さ
                // (例: 100px)まで圧縮されてしまう。子である各log-line行はflex-shrink:0を
                // 明示しているため実際には縮まず親からはみ出すが、contentContainer.layout.height
                // 自体は圧縮後の(=ビューポートと同じ)値のまま報告され、verticalScroller.highValue
                // の算出元であるboundingBox(実際の子の広がり)との間に食い違いが生まれる。この
                // 結果、末尾までスクロールしても最後の行がビューポートの高さ分しか表示されず、
                // 実際のログ末尾数十px(概ね1行未満)が常に見切れる不具合の根本原因だった
                // (#122、Opus協力の上で確認)。テーマの有無に関わらず圧縮されないよう明示指定する.
                var contentContainer = LogScrollView.contentContainer;
                if (contentContainer != null)
                {
                    contentContainer.style.flexShrink = 0;
                    contentContainer.style.flexGrow = 0;
                    contentContainer.style.minHeight = StyleKeyword.Auto;
                }

                // 縦スクロールバーが既定(Auto)だと、ログ行数がビューポートに収まるかどうかで
                // ビューポート幅(=WhiteSpace.Normalで折り返すLabelのavailable width)が変動する。
                // 幅が変わるとテキストの折り返し高さの再計測が必要になり、Yogaのレイアウトパスが
                // 1回で収束せず、contentContainerの高さ(≒verticalScroller.highValue)が実際の
                // 子要素の広がりより小さいまま取り残される、行同士が重なって表示される、という
                // 2つの不具合の一因になっていた(#122、Opus協力の上で確認)。常時表示にして
                // 幅変動の要因そのものを無くす.
                LogScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
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
            // flex-basis:auto(既定)のままだと基準サイズが「中身(全ログ行)の高さ」になり、
            // ログが増えるほどScrollView自身の箱が親の残り領域より背が高くなる(スクロール
            // コンテナにflex-basis:0とmin-height:0が必須という既知のフレックスボックスの罠)。
            // これによりverticalScroller.highValueが常に本来より小さく算出され、マウスホイール
            // 感度に関わらず末尾の数行に到達できなくなる不具合の根本原因だった(#122)。
            LogScrollView.style.flexBasis = 0;
            LogScrollView.style.minHeight = 0;

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

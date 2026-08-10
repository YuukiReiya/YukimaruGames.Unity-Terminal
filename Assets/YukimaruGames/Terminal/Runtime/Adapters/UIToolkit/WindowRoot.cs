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
        public const string LogScrollViewName = "log-scroll-view";
        public const string InputFieldName = "input-field";
        public const string SubmitButtonName = "submit-button";
        public const string PromptLabelName = "prompt-label";
        public const string LauncherContainerName = "launcher-container";
        public const string LauncherOpenButtonName = "launcher-open-button";
        public const string LauncherCloseButtonName = "launcher-close-button";

        private UIDocument _document;

        public VisualElement Root { get; private set; }
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

            Root = rootVisualElement.Q<VisualElement>(RootName) ?? rootVisualElement;
            LogScrollView = rootVisualElement.Q<ScrollView>(LogScrollViewName);
            InputField = rootVisualElement.Q<TextField>(InputFieldName);
            SubmitButton = rootVisualElement.Q<Button>(SubmitButtonName);
            PromptLabel = rootVisualElement.Q<Label>(PromptLabelName);
            LauncherContainer = rootVisualElement.Q<VisualElement>(LauncherContainerName);
            LauncherOpenButton = rootVisualElement.Q<Button>(LauncherOpenButtonName);
            LauncherCloseButton = rootVisualElement.Q<Button>(LauncherCloseButtonName);

            Root.style.position = Position.Absolute;

            IsInitialized = true;
        }

        /// <summary>
        /// <see cref="TerminalRect"/>をルート<see cref="VisualElement"/>のstyleへ反映する.
        /// </summary>
        public void ApplyRect(TerminalRect rect)
        {
            if (Root == null) return;

            Root.style.left = rect.X;
            Root.style.top = rect.Y;
            Root.style.width = rect.Width;
            Root.style.height = rect.Height;
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

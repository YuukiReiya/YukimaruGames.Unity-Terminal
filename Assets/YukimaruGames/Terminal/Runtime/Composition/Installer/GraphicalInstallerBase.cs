#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Composition.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Presentation.Accessors;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Coordinators;
using YukimaruGames.Terminal.Presentation.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 描画を持つUIバックエンド(IMGUI / UIToolkit / uGUI等)に共通する構築フローをまとめた基底クラス.
    /// </summary>
    /// <remarks>
    /// <see cref="InstallerBase"/>の抽象点<c>BuildBackend</c>を、
    /// 「描画コンテキストの構築 + Coordinatorの構築」として実装する。派生クラスが実装するのは
    /// <see cref="BuildRenderingContext"/>だけでよい。
    ///
    /// <c>_theme</c>はここには置かない。IMGUI版はGUIStyleを実行時に組み立てるためテーマが要るが、
    /// UIToolkit版・uGUI版の見た目はUSSやprefabといったアセット側で決まり、テーマを
    /// Inspectorから動的に設定する必然性が薄いため、テーマを必要とするバックエンドが
    /// 各自で宣言する(#145).
    /// </remarks>
    [Serializable]
    public abstract class GraphicalInstallerBase : InstallerBase
    {
        #region inner-struct

        /// <summary>
        /// プレゼンテーション層のパラメータをとりまとめたContext.
        /// </summary>
        protected struct RenderingContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components { get; set; }

            /// <inheritdoc cref="ITerminalGUI"/>
            public ITerminalGUI GUI { get; set; }
            /// <inheritdoc cref="IScrollMutator"/>
            public IScrollMutator ScrollMutator { get; set; }
            /// <inheritdoc cref="IWindowAnimationAccessor"/>
            public IWindowAnimationAccessor WindowAnimationAccessor { get; set; }
            /// <inheritdoc cref="IWindowPresenter"/>
            public IWindowPresenter WindowPresenter { get; set; }
            /// <inheritdoc cref="IInputPresenter"/>
            public IInputPresenter InputPresenter { get; set; }
            /// <inheritdoc cref="ILogPresenter"/>
            public ILogPresenter LogPresenter { get; set; }
            /// <inheritdoc cref="ISubmitPresenter"/>
            public ISubmitPresenter SubmitPresenter { get; set; }
            /// <inheritdoc cref="ILauncherPresenter"/>
            public ILauncherPresenter LauncherPresenter { get; set; }
            /// <inheritdoc cref="ITerminalView"/>
            public ITerminalView View { get; set; }
        }

        /// <summary>
        /// orchestratorをとりまとめたコンテキスト
        /// </summary>
        private struct CoordinatorContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components { get; set; }

            /// <inheritdoc cref="Coordinator"/>
            public TerminalCoordinator Coordinator { get; set; }
            /// <inheritdoc cref="IEventListener"/>
            public IEventListener EventListener { get; set; }
        }

        #endregion

        [SerializeReference, SerializeInterface]
        private ITerminalAnimation _animation = new ImmediateModeAnimation();

        #region runtime-instances

        /// <summary>
        /// ウィンドウ開閉アニメーションの状態。<see cref="ApplyAnimation"/>で再適用対象になる.
        /// </summary>
        protected WindowAnimationAccessor WindowAnimation { get; set; }

        /// <summary>
        /// ランチャーボタンの表示状態。<see cref="ApplyOptions"/>で再適用対象になる.
        /// </summary>
        protected LauncherVisibleAccessor LauncherVisibility { get; set; }

        /// <summary>
        /// プロンプト描画。<see cref="ApplyOptions"/>でローディング表示設定の再適用対象になる.
        /// </summary>
        protected IPromptRenderer PromptRenderer { get; set; }

        #endregion

        /// <inheritdoc/>
        /// <remarks>
        /// 描画コンテキストとCoordinatorを構築し、両者のComponentsを束ねて返す。
        /// 骨格をすり抜けられないよう<c>sealed</c>にしてある。UIバックエンドが実装するのは
        /// <see cref="BuildRenderingContext"/>だけ.
        /// </remarks>
        protected sealed override BackendContext BuildBackend(ITerminalOptions options, in DomainContext domain)
        {
            var animation = _animation ?? new NullAnimation();

            var rendering = BuildRenderingContext(animation, options, in domain);
            var coordinator = BuildCoordinatorContext(in domain, in rendering, options);

            return new BackendContext
            {
                Components = rendering.Components.Concat(coordinator.Components).ToArray(),
                GUI = rendering.GUI,
                View = rendering.View,
            };
        }

        /// <summary>
        /// バックエンド固有の描画コンテキスト(Renderer/Presenter/View)を構築する.
        /// </summary>
        /// <remarks>
        /// <see cref="WindowAnimation"/>等、基底が再同期の対象とするインスタンスを生成した場合は、
        /// ここで併せて代入すること(<see cref="CreateWindowAnimationAccessor"/> /
        /// <see cref="CreateLauncherVisibleAccessor"/>を使えば代入まで済む).
        /// </remarks>
        protected abstract RenderingContext BuildRenderingContext(
            ITerminalAnimation animation,
            ITerminalOptions options,
            in DomainContext domain);

        /// <inheritdoc/>
        protected override void OnResolve()
        {
            base.OnResolve();

            ApplyAnimation(_animation ?? new NullAnimation());
        }

        /// <inheritdoc/>
        protected override void ApplyOptions(ITerminalOptions options)
        {
            base.ApplyOptions(options);

            if (LauncherVisibility != null)
            {
                LauncherVisibility.IsVisible = options.IsButtonVisible;
                LauncherVisibility.IsReverse = options.IsButtonReverse;
            }

            if (PromptRenderer != null)
            {
                PromptRenderer.ShowLoadingIndicator = options.ShowLoadingIndicator;
                PromptRenderer.LoadingIndicatorFrames = options.LoadingIndicatorFrames;
            }
        }

        /// <summary>
        /// アニメーション設定を実行時インスタンスへ再適用する.
        /// </summary>
        protected virtual void ApplyAnimation(ITerminalAnimation animation)
        {
            if (WindowAnimation == null) return;

            WindowAnimation.Anchor = animation.Anchor;
            WindowAnimation.Style = animation.WindowStyle;
            WindowAnimation.Duration = animation.Duration;
            WindowAnimation.Scale = animation.CompactScale;
        }

        /// <inheritdoc/>
        protected override void ClearReferences()
        {
            WindowAnimation = null;
            LauncherVisibility = null;
            PromptRenderer = null;

            base.ClearReferences();
        }

        /// <summary>
        /// アニメーション設定から<see cref="WindowAnimationAccessor"/>を生成し、
        /// <see cref="WindowAnimation"/>へ設定する.
        /// </summary>
        protected WindowAnimationAccessor CreateWindowAnimationAccessor(ITerminalAnimation animation)
        {
            WindowAnimation = new WindowAnimationAccessor
            {
                State = animation.BootupWindowState,
                Anchor = animation.Anchor,
                Style = animation.WindowStyle,
                Duration = animation.Duration,
                Scale = animation.CompactScale,
            };

            return WindowAnimation;
        }

        /// <summary>
        /// オプションから<see cref="LauncherVisibleAccessor"/>を生成し、
        /// <see cref="LauncherVisibility"/>へ設定する.
        /// </summary>
        protected LauncherVisibleAccessor CreateLauncherVisibleAccessor(ITerminalOptions options)
        {
            LauncherVisibility = new LauncherVisibleAccessor
            {
                IsVisible = options.IsButtonVisible,
                IsReverse = options.IsButtonReverse,
            };

            return LauncherVisibility;
        }

        private InputKeyboardType ResolveKeyboardType(ITerminalOptions options)
        {
#if ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            return options.Input.InputKeyboardType;
#elif ENABLE_INPUT_SYSTEM
            return InputKeyboardType.InputSystem;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return InputKeyboardType.Legacy;
#else
            return InputKeyboardType.None;
#endif
        }

        private IKeyboardInputHandler CreateInputHandler(ITerminalOptions options, InputKeyboardType resultType)
        {
            var input = options.Input;
            var factory =
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(input.InputSystemKey, input.LegacyInputKey, input.TriggerTiming, input.Priority);
#elif ENABLE_INPUT_SYSTEM
                new TerminalKeyboardFactory(input.InputSystemKey, input.TriggerTiming, input.Priority);
#elif ENABLE_LEGACY_INPUT_MANAGER
                new TerminalKeyboardFactory(input.LegacyInputKey, input.TriggerTiming, input.Priority);
#else
                new TerminalKeyboardFactory();
#endif
            return factory.Create(resultType);
        }

        private IWindowFocusInputGuard CreateWindowFocusInputGuard(ITerminalOptions options, InputKeyboardType resultType)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (resultType is InputKeyboardType.Legacy && options.Input.AllowKeyInputWhileTextFieldFocused)
            {
                return new LegacyTextFieldKeyEatingGuard();
            }
#endif
            return NullWindowFocusInputGuard.Instance;
        }

        private CoordinatorContext BuildCoordinatorContext(
            in DomainContext domain,
            in RenderingContext rendering,
            ITerminalOptions options)
        {
            var keyboardType = ResolveKeyboardType(options);
            var inputHandler = CreateInputHandler(options, keyboardType);
            var eventListener = new EventListener(inputHandler);
            var windowFocusInputGuard = CreateWindowFocusInputGuard(options, keyboardType);

            var coordinator = new TerminalCoordinator(
                domain.Service,
                rendering.GUI,
                rendering.ScrollMutator,
                rendering.WindowAnimationAccessor,
                rendering.WindowPresenter,
                rendering.InputPresenter,
                rendering.LogPresenter,
                rendering.SubmitPresenter,
                rendering.LauncherPresenter,
                eventListener,
                windowFocusInputGuard);

            return new CoordinatorContext
            {
                Coordinator = coordinator,
                EventListener = eventListener,
                Components = new object[]
                {
                    coordinator,
                    eventListener,
                }
            };
        }
    }
}

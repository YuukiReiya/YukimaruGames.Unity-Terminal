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
using System.Reflection;
using System.Linq;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Infrastructure.Factories;
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

            RegisterWindowCommands(in domain, rendering.InputPresenter, coordinator.Coordinator);

            // フォントサイズは画面の高さに対する比率で決まるため(ThemeBinder.ResolveFontSize)、
            // 解像度やウィンドウサイズが変わったら再適用する。ウィンドウ矩形側は
            // TerminalCoordinatorが同じ通知でRefreshしており、そちらと歩調を合わせる.
            _gui = rendering.GUI;
            if (_gui != null) _gui.OnScreenSizeChanged += HandleScreenSizeChanged;

            return new BackendContext
            {
                Components = rendering.Components.Concat(coordinator.Components).ToArray(),
                GUI = rendering.GUI,
                View = rendering.View,
            };
        }

        /// <summary>
        /// キー操作やボタンに手が届かない構成のための、逃げ道となる組み込みコマンドを登録する.
        /// </summary>
        /// <remarks>
        /// 詳細は<see cref="WindowCommands"/>を参照。
        /// 入力欄を持たないCLIバックエンドには不要なため、ここ(グラフィカルなバックエンドの基底)で
        /// 登録する.
        /// </remarks>
        private static void RegisterWindowCommands(
            in DomainContext domain,
            IInputPresenter inputPresenter,
            TerminalCoordinator coordinator)
        {
            if (inputPresenter == null || domain.Registry == null) return;

            var commands = new WindowCommands(inputPresenter, coordinator);

            Register(in domain, commands, WindowCommands.UnfocusMethod, WindowCommands.UnfocusMeta);
            Register(in domain, commands, WindowCommands.CloseMethod, WindowCommands.CloseMeta);
        }

        private static void Register(in DomainContext domain, object instance, MethodInfo method, in CommandMeta meta)
        {
            var handler = CommandFactory.Create(instance, method, meta, ModeServiceBundle.Empty);

            if (domain.Registry.Add(meta.Command, handler))
            {
                domain.Autocomplete?.Register(meta.Command);
            }
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
        /// <summary>画面サイズの変化を受け取るために保持する(解除のため).</summary>
        [NonSerialized] private ITerminalGUI _gui;

        /// <summary>描画側が観測した画面の高さ(px). 0は「未観測」.</summary>
        [NonSerialized] private int _observedScreenHeight;

        /// <summary>
        /// フォントサイズの算出に使う画面の高さ(px).
        /// </summary>
        /// <remarks>
        /// <b><see cref="Screen"/>を直接読んではならない。</b>Inspectorでの変更(OnValidate)のように
        /// エディタ側のコンテキストから呼ばれた場合、<see cref="Screen"/>は<b>Game Viewではなく
        /// その時アクティブなエディタウィンドウのサイズ</b>を返す(実測で40や782など、無関係な値が
        /// 返ることを確認)。そのまま拡縮に使うと、実行中にInspectorでフォントサイズを変えた瞬間に
        /// 別の倍率が適用され、拡縮が外れたように見える。
        /// <para>
        /// 描画側(<see cref="ITerminalGUI"/>)が実際のレンダリング中に観測した値を使う。
        /// 観測前(初期化直後)だけは<see cref="Screen"/>にフォールバックするが、
        /// 最初の描画で通知が来た時点で正しい値へ置き換わる.
        /// </para>
        /// </remarks>
        protected int ScreenHeight => _observedScreenHeight > 0 ? _observedScreenHeight : Screen.height;

        /// <summary>
        /// 画面サイズが変わったら、テーマ・設定一式を再適用する.
        /// </summary>
        /// <remarks>
        /// 再適用の経路は<see cref="OnResolve"/>(Inspectorでの変更を反映する経路)と同じものを使う。
        /// フォントサイズだけを更新する専用経路を増やすと、バックエンドごとに更新漏れが起きるため.
        /// </remarks>
        private void HandleScreenSizeChanged(Vector2Int size)
        {
            _observedScreenHeight = size.y;
            OnResolve();
        }

        /// <inheritdoc/>
        protected override void ClearReferences()
        {
            if (_gui != null)
            {
                _gui.OnScreenSizeChanged -= HandleScreenSizeChanged;
                _gui = null;
            }

            _observedScreenHeight = 0;

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

        /// <summary>
        /// 有効な入力方式を解決する.
        /// </summary>
        /// <remarks>
        /// バックエンド側でも入力方式に応じた分岐が要る場合がある
        /// (uGUI版はEventSystemの入力モジュールをLegacy/InputSystemで選び分ける必要がある。
        /// Active Input HandlingがInput System専用の環境でStandaloneInputModuleを使うと
        /// 実行時例外になるため)。そのためprotectedで公開している.
        /// </remarks>
        protected InputKeyboardType ResolveKeyboardType(ITerminalOptions options)
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

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
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.IMGUI.Accessors;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Repositories;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Infrastructure.Discoverer;
using YukimaruGames.Terminal.Infrastructure.Factories;
using YukimaruGames.Terminal.Infrastructure.Modes;
using YukimaruGames.Terminal.Presentation.Accessors;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Presentation.Coordinators;
using YukimaruGames.Terminal.Presentation.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// UIバックエンド(IMGUI / UIToolkit等)ごとの<see cref="IInstaller"/>実装に共通する
    /// 構築フローをまとめた基底クラス.
    /// </summary>
    /// <remarks>
    /// Install()の骨格(Domain構築 → コマンド登録 → 描画構築 → Coordinator構築 → Scope構築、
    /// および失敗時のCleanUp)と、バックエンドに依存しないDomain層・入力・Scopeの構築、
    /// Inspector設定(<see cref="ITerminalTheme"/>/<see cref="ITerminalAnimation"/>/
    /// <see cref="ITerminalOptions"/>)の再同期をここに集約する。
    /// バックエンド固有なのは描画コンテキストの構築(<see cref="BuildRenderingContext"/>)と、
    /// そこで生成した固有インスタンスの後始末・テーマ適用(<see cref="ClearReferences"/> /
    /// <see cref="SyncTheme"/>)だけであり、派生クラスはそこだけを実装すればよい(#137).
    ///
    /// 派生クラスの型名・名前空間・アセンブリは変更しないこと。<c>SerializeReference</c>は
    /// シーン/プレハブへ{class, ns, asm}を保存するため、これらが変わると既存シーンの
    /// Installer参照が壊れる(基底クラスを挟むだけなら影響しない).
    /// </remarks>
    [Serializable]
    public abstract class InstallerBase : IInstaller
    {
        #region inner-struct

        /// <summary>
        /// ドメイン層のパラメータをとりまとめたContext
        /// </summary>
        protected struct DomainContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components;

            /// <inheritdoc cref="ITerminalService"/>
            public ITerminalService Service;
            /// <inheritdoc cref="ICommandLogger"/>
            public ICommandLogger Logger;
            /// <inheritdoc cref="ICommandHistory"/>
            public ICommandHistory History;
            /// <inheritdoc cref="ICommandRegistry"/>
            public ICommandRegistry Registry;
            /// <inheritdoc cref="ICommandAutocomplete"/>
            public ICommandAutocomplete Autocomplete;
            /// <inheritdoc cref="ICommandDiscoverer"/>
            public ICommandDiscoverer Discoverer;
            /// <inheritdoc cref="IExecuteCommandUseCase"/>
            public IExecuteCommandUseCase UseCase;
        }

        /// <summary>
        /// プレゼンテーション層のパラメータをとりまとめたContext
        /// </summary>
        protected struct RenderingContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components;

            /// <inheritdoc cref="ITerminalGUI"/>
            public ITerminalGUI GUI;
            /// <inheritdoc cref="IScrollMutator"/>
            public IScrollMutator ScrollMutator;
            /// <inheritdoc cref="IWindowAnimationAccessor"/>
            public IWindowAnimationAccessor WindowAnimationAccessor;
            /// <inheritdoc cref="IWindowPresenter"/>
            public IWindowPresenter WindowPresenter;
            /// <inheritdoc cref="IInputPresenter"/>
            public IInputPresenter InputPresenter;
            /// <inheritdoc cref="ILogPresenter"/>
            public ILogPresenter LogPresenter;
            /// <inheritdoc cref="ISubmitPresenter"/>
            public ISubmitPresenter SubmitPresenter;
            /// <inheritdoc cref="ILauncherPresenter"/>
            public ILauncherPresenter LauncherPresenter;
            /// <inheritdoc cref="ITerminalView"/>
            public ITerminalView View { get; set; }
        }

        /// <summary>
        /// orchestratorをとりまとめたコンテキスト
        /// </summary>
        protected struct CoordinatorContext
        {
            /// <summary>
            /// 構成データ
            /// </summary>
            public IReadOnlyList<object> Components;

            /// <inheritdoc cref="Coordinator"/>
            public TerminalCoordinator Coordinator;
            /// <inheritdoc cref="IEventListener"/>
            public IEventListener EventListener;
        }

        #endregion

        [SerializeReference, SerializeInterface]
        private ITerminalTheme _theme = new ImmediateModeTheme();

        [SerializeReference, SerializeInterface]
        private ITerminalAnimation _animation = new ImmediateModeAnimation();

        [SerializeReference, SerializeInterface]
        private ITerminalOptions _options = new ImmediateModeOptions();

        #region runtime-instances

        /// <summary>
        /// ログ種別ごとの色。<see cref="SyncTheme"/>でテーマ色の再適用対象になる.
        /// </summary>
        protected ColorPaletteAccessor ThemeColorPalette { get; set; }

        /// <summary>
        /// キャレットの点滅速度。<see cref="SyncTheme"/>でテーマ値の再適用対象になる.
        /// </summary>
        protected CursorFlashSpeedAccessor CursorFlash { get; set; }

        /// <summary>
        /// ウィンドウ開閉アニメーションの状態。<see cref="SyncAnimation"/>で再適用対象になる.
        /// </summary>
        protected WindowAnimationAccessor WindowAnimation { get; set; }

        /// <summary>
        /// ランチャーボタンの表示状態。<see cref="SyncOptions"/>で再適用対象になる.
        /// </summary>
        protected LauncherVisibleAccessor LauncherVisibility { get; set; }

        /// <summary>
        /// プロンプト描画。<see cref="SyncOptions"/>でローディング表示設定の再適用対象になる.
        /// </summary>
        protected IPromptRenderer PromptRenderer { get; set; }

        /// <summary>
        /// 既定モード。<see cref="BuildDomainContext"/>が生成し、<see cref="SyncOptions"/>で
        /// プロンプト文字列の再適用対象になる.
        /// </summary>
        protected NormalMode Mode { get; private set; }

        #endregion

        TerminalRuntimeScope IInstaller.Install()
        {
            // Null Object Pattern: 意図的な null は Null 実装にフォールバック
            var theme = _theme ?? new NullTheme();
            var animation = _animation ?? new NullAnimation();
            var options = _options ?? new NullOptions();

            DomainContext domainContext = default;
            RenderingContext renderingContext = default;
            CoordinatorContext coordinatorContext = default;

            try
            {
                domainContext = BuildDomainContext(options);
                RegisterCommands(in domainContext);
                renderingContext = BuildRenderingContext(theme, animation, options, in domainContext);
                coordinatorContext = BuildCoordinatorContext(in domainContext, in renderingContext, options);
                return BuildScope(in domainContext, in renderingContext, in coordinatorContext);
            }
            catch (Exception)
            {
                CleanUp(domainContext.Components);
                CleanUp(renderingContext.Components);
                CleanUp(coordinatorContext.Components);
                ClearReferences();
                throw;
            }
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            try
            {
                (scope as IDisposable)?.Dispose();
            }
            finally
            {
                ClearReferences();
            }
        }

        async ValueTask IInstaller.UninstallAsync(TerminalRuntimeScope scope)
        {
            try
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    (scope as IDisposable)?.Dispose();
                }
            }
            finally
            {
                ClearReferences();
            }
        }

        void IInstaller.Resolve(TerminalRuntimeScope scope)
        {
            if (scope == null) return;

            var theme = _theme ?? new NullTheme();
            var animation = _animation ?? new NullAnimation();
            var options = _options ?? new NullOptions();

            SyncTheme(theme);
            SyncAnimation(animation);
            SyncOptions(options);
        }

        /// <summary>
        /// バックエンド固有の描画コンテキスト(Renderer/Presenter/View)を構築する.
        /// </summary>
        /// <remarks>
        /// <see cref="ThemeColorPalette"/>等、基底が再同期の対象とするインスタンスを生成した場合は、
        /// ここで併せて代入すること.
        /// </remarks>
        protected abstract RenderingContext BuildRenderingContext(
            ITerminalTheme theme,
            ITerminalAnimation animation,
            ITerminalOptions options,
            in DomainContext domain);

        /// <summary>
        /// 保持している実行時インスタンスの参照を解放する.
        /// </summary>
        /// <remarks>
        /// 破棄そのものは<see cref="TerminalRuntimeScope"/>(Components経由のDispose)が行う。
        /// ここでの責務は参照を残さないことであり、派生クラスは自身が保持する参照をクリアしたうえで
        /// 基底実装を呼ぶこと.
        /// </remarks>
        protected virtual void ClearReferences()
        {
            ThemeColorPalette = null;
            CursorFlash = null;
            WindowAnimation = null;
            LauncherVisibility = null;
            PromptRenderer = null;
            Mode = null;
        }

        /// <summary>
        /// テーマ設定を実行時インスタンスへ再適用する.
        /// </summary>
        /// <remarks>
        /// 基底はバックエンド非依存の同期先(<see cref="ThemeColorPalette"/> /
        /// <see cref="CursorFlash"/>)のみを扱う。GUIStyleやVisualElementへの色適用のような
        /// バックエンド固有の反映は派生クラスで行い、基底実装を併せて呼ぶこと.
        /// </remarks>
        protected virtual void SyncTheme(ITerminalTheme theme)
        {
            if (ThemeColorPalette != null)
            {
                ThemeColorPalette[Definitions.ThemeLabel.Message] = theme.MessageColor;
                ThemeColorPalette[Definitions.ThemeLabel.Entry] = theme.EntryColor;
                ThemeColorPalette[Definitions.ThemeLabel.Warning] = theme.WarningColor;
                ThemeColorPalette[Definitions.ThemeLabel.Error] = theme.ErrorColor;
                ThemeColorPalette[Definitions.ThemeLabel.Assert] = theme.AssertColor;
                ThemeColorPalette[Definitions.ThemeLabel.Exception] = theme.ExceptionColor;
                ThemeColorPalette[Definitions.ThemeLabel.System] = theme.SystemColor;
                ThemeColorPalette[Definitions.ThemeLabel.Cursor] = theme.CaretColor;
                ThemeColorPalette[Definitions.ThemeLabel.Selection] = theme.SelectionColor;
            }

            if (CursorFlash != null)
            {
                CursorFlash.FlashSpeed = theme.CursorFlashSpeed;
            }
        }

        /// <summary>
        /// アニメーション設定を実行時インスタンスへ再適用する.
        /// </summary>
        protected virtual void SyncAnimation(ITerminalAnimation animation)
        {
            if (WindowAnimation == null) return;

            WindowAnimation.Anchor = animation.Anchor;
            WindowAnimation.Style = animation.WindowStyle;
            WindowAnimation.Duration = animation.Duration;
            WindowAnimation.Scale = animation.CompactScale;
        }

        /// <summary>
        /// 各種オプションを実行時インスタンスへ再適用する.
        /// </summary>
        protected virtual void SyncOptions(ITerminalOptions options)
        {
            if (LauncherVisibility != null)
            {
                LauncherVisibility.IsVisible = options.IsButtonVisible;
                LauncherVisibility.IsReverse = options.IsButtonReverse;
            }

            if (Mode != null)
            {
                Mode.Prompt = options.Prompt;
            }

            if (PromptRenderer != null)
            {
                PromptRenderer.ShowLoadingIndicator = options.ShowLoadingIndicator;
                PromptRenderer.LoadingIndicatorFrames = options.LoadingIndicatorFrames;
            }
        }

        /// <summary>
        /// テーマのログ色から<see cref="ColorPaletteAccessor"/>を生成し、
        /// <see cref="ThemeColorPalette"/>へ設定する.
        /// </summary>
        protected ColorPaletteAccessor CreateColorPaletteAccessor(ITerminalTheme theme)
        {
            ThemeColorPalette = new ColorPaletteAccessor(new Dictionary<string, Color>
            {
                { Definitions.ThemeLabel.Message, theme.MessageColor },
                { Definitions.ThemeLabel.Entry, theme.EntryColor },
                { Definitions.ThemeLabel.Warning, theme.WarningColor },
                { Definitions.ThemeLabel.Error, theme.ErrorColor },
                { Definitions.ThemeLabel.Assert, theme.AssertColor },
                { Definitions.ThemeLabel.Exception, theme.ExceptionColor },
                { Definitions.ThemeLabel.System, theme.SystemColor },
                { Definitions.ThemeLabel.Cursor, theme.CaretColor },
                { Definitions.ThemeLabel.Selection, theme.SelectionColor },
            });

            return ThemeColorPalette;
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
        /// テーマから<see cref="CursorFlashSpeedAccessor"/>を生成し、
        /// <see cref="CursorFlash"/>へ設定する.
        /// </summary>
        protected CursorFlashSpeedAccessor CreateCursorFlashSpeedAccessor(ITerminalTheme theme)
        {
            CursorFlash = new CursorFlashSpeedAccessor(theme.CursorFlashSpeed);
            return CursorFlash;
        }

        private static void CleanUp(IReadOnlyList<object> components)
        {
            if (components == null)
            {
                return;
            }

            // Interface 越しの foreach による GC Alloc を避けるため、for で列挙
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] is IDisposable component)
                {
                    component.Dispose();
                }
            }
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

        private DomainContext BuildDomainContext(ITerminalOptions options)
        {
            var logger = new CommandLogger(options.BufferSize);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var discover = new CommandDiscoverer(logger, new[] { "Assembly-CSharp" }.Concat(options.AdditionalCommandAssemblies ?? Array.Empty<string>()));
            var autocomplete = new CommandAutocomplete();
            var normalMode = new NormalMode(logger, registry, invoker, parser, history, autocomplete) { Prompt = options.Prompt };
            Mode = normalMode;
            var modeCommandBinder = new ModeCommandBinder(discover, () => new CommandRegistry(logger), logger);
            var executeCommandUseCase = new ExecuteCommandUseCase(logger, normalMode, modeCommandBinder);
            var service = new TerminalService(
                logger,
                registry,
                autocomplete,
                executeCommandUseCase
            );

            return new DomainContext
            {
                Components = new object[] { logger, registry, history, autocomplete, discover, executeCommandUseCase, service },
                Logger = logger,
                Registry = registry,
                History = history,
                Autocomplete = autocomplete,
                Discoverer = discover,
                Service = service,
                UseCase = executeCommandUseCase,
            };
        }

        private void RegisterCommands(in DomainContext domain)
        {
            // static コマンドから ITerminalModeStack を注入可能にする(python等の入場コマンド、
            // terminal.stack 等の診断コマンド用)。ITerminalService丸ごとは注入しない
            // (ExecuteAsync等を誤って呼ぶとディスパッチャの排他ロックでデッドロックするため).
            var services = new Dictionary<Type, object>
            {
                { typeof(IModeStackInspector), domain.UseCase },
                { typeof(IModeOutput), domain.UseCase.Output },
                { typeof(IModeTransitionRequestSink), domain.UseCase.Transitions },
                { typeof(ICommandRegistry), domain.Registry },
                { typeof(ICommandLogger), domain.Logger },
            };
            var bundle = new ModeServiceBundle(services);

            var specs = domain.Discoverer.Discover();
            foreach (var spec in specs)
            {
                var handler = CommandFactory.Create(spec.Method, bundle);
                if (domain.Registry.Add(spec.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(spec.Meta.Command);
                }
            }

            // terminal.stack等のパッケージ内蔵コマンドは、Assembly-CSharpの参照グラフ次第で
            // 属性発見(ICommandDiscoverer.Discover)に乗らない場合がある(利用者コードが実際に
            // 型を参照していないアセンブリはAssemblyRefに現れないため)。Composition層は
            // Infrastructureを直接知っているので、確実性のためここで直接登録する.
            RegisterBuiltinCommands(domain, bundle);
        }

        private void RegisterBuiltinCommands(in DomainContext domain, in ModeServiceBundle bundle)
        {
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinDiagnosticsCommands.Methods);
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinGeneralCommands.Methods);

#if UNITY_EDITOR
            // Editor限定コマンドは実機ビルド(UNITY_EDITOR未定義)では型ごとコンパイル対象外になる
            // ため、この呼び出し自体も#if UNITY_EDITORで囲い、実機ビルドに参照を残さない.
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinEditorCommands.Methods);
#endif
        }

        private static void RegisterBuiltinCommandMethods(in DomainContext domain, in ModeServiceBundle bundle, MethodInfo[] methods)
        {
            foreach (var method in methods)
            {
                var handler = CommandFactory.Create(method, bundle);
                if (domain.Registry.Add(handler.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(handler.Meta.Command);
                }
            }
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

        private TerminalRuntimeScope BuildScope(
            in DomainContext domain,
            in RenderingContext rendering,
            in CoordinatorContext coordinator)
        {
            var instances =
                domain.Components
                    .Concat(rendering.Components)
                    .Concat(coordinator.Components).ToArray();

            var updatables = instances.OfType<IUpdatable>().ToList();
            var asyncDisposables = instances.OfType<IAsyncDisposable>().ToList();
            var disposables = instances.OfType<IDisposable>().Where(d => d is not IAsyncDisposable).ToList();

            var entryPoint = new TerminalEntryPoint(updatables, rendering.GUI);

            return new TerminalRuntimeScope(
                entryPoint,
                domain.Service,
                domain.Registry,
                domain.Autocomplete,
                rendering.View,
                disposables,
                asyncDisposables,
                domain.Logger);
        }
    }
}

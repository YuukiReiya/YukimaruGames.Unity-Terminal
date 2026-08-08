using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.ExternalTerminal;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Repositories;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Infrastructure.Discoverer;
using YukimaruGames.Terminal.Infrastructure.Factories;
using YukimaruGames.Terminal.Infrastructure.Modes;
using YukimaruGames.Terminal.Infrastructure.Repositories;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// CMD/zsh等の外部ターミナルプロセスを起動し、そこへコマンド入出力を中継するInstaller.
    /// </summary>
    /// <remarks>
    /// <see cref="TerminalStandardInstaller"/>がIMGUIレンダリング一式(Renderer/Presenter/Coordinator/
    /// TerminalIMGUI)を構築するのに対し、こちらはコマンド実行系(Domain/Application層)のみを構築し、
    /// 描画は行わない(ゲーム内ウィンドウを持たないため<see cref="ITerminalView"/>はNull実装を使う)。
    /// 「どちらのViewを使うか」は<see cref="TerminalBootstrapper"/>の_installerフィールド
    /// (SerializeReferenceの型選択メニュー)で切り替える想定.
    /// </remarks>
    [Serializable, AddTypeMenu("External(cmd,zsh)")]
    public sealed class TerminalExternalInstaller : IInstaller
    {
        /// <summary>
        /// ドメイン層のパラメータをとりまとめたContext
        /// </summary>
        private struct DomainContext
        {
            public IReadOnlyList<object> Components;
            public ITerminalService Service;
            public ICommandLogger Logger;
            public ICommandHistory History;
            public ICommandRegistry Registry;
            public ICommandAutocomplete Autocomplete;
            public ICommandDiscoverer Discoverer;
            public IExecuteCommandUseCase UseCase;
        }

        [SerializeReference, SerializeInterface]
        private ITerminalOptions _options = new TerminalExternalOptions();

        [NonSerialized] private NormalMode _normalMode;

        TerminalRuntimeScope IInstaller.Install()
        {
            // Unity Editorの仕様上、SerializeReferenceな_installerフィールドの型をInspector上の
            // 型選択メニューで切り替えた直後は、ネストした_optionsフィールドがユーザーの操作意図に
            // 反してnullのまま復元されることがある(既知のシリアライズ上の癖。実際に検証で再現した)。
            // TerminalNullOptions(BufferSize=0)へフォールバックするとCommandLoggerの実効バッファが
            // 1件まで縮んで外部ターミナルとして機能しなくなるため、フォールバック先も
            // 専用設定(TerminalExternalOptions)の既定値にする.
            var options = _options ?? new TerminalExternalOptions();

            DomainContext domainContext = default;
            ExternalTerminalSession session = null;

            try
            {
                domainContext = BuildDomainContext(options);
                RegisterCommands(in domainContext);

                session = new ExternalTerminalSession(domainContext.Service);
                session.Open();

                var entryPoint = new TerminalEntryPoint(Array.Empty<IUpdatable>(), null);

                var instances = domainContext.Components.Append((object)session).ToArray();
                var asyncDisposables = instances.OfType<IAsyncDisposable>().ToList();
                var disposables = instances.OfType<IDisposable>().Where(d => d is not IAsyncDisposable).ToList();

                return new TerminalRuntimeScope(
                    entryPoint,
                    domainContext.Service,
                    domainContext.Registry,
                    domainContext.Autocomplete,
                    new NullTerminalView(),
                    disposables,
                    asyncDisposables,
                    domainContext.Logger);
            }
            catch (Exception)
            {
                session?.Dispose();

                if (domainContext.Components != null)
                {
                    for (var i = 0; i < domainContext.Components.Count; i++)
                    {
                        (domainContext.Components[i] as IDisposable)?.Dispose();
                    }
                }

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

        async System.Threading.Tasks.ValueTask IInstaller.UninstallAsync(TerminalRuntimeScope scope)
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

            var options = _options ?? new TerminalNullOptions();
            if (_normalMode != null)
            {
                _normalMode.Prompt = options.Prompt;
            }
        }

        private void ClearReferences()
        {
            _normalMode = null;
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
            _normalMode = normalMode;
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

            RegisterBuiltinCommands(domain, bundle);
        }

        private void RegisterBuiltinCommands(in DomainContext domain, in ModeServiceBundle bundle)
        {
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinDiagnosticsCommands.Methods);
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinGeneralCommands.Methods);

#if UNITY_EDITOR
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
    }
}

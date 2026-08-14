using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// ドメイン層の構築とコマンド登録を担う.
    /// </summary>
    /// <remarks>
    /// バックエンドの種類に依存しないため、継承ではなく静的ヘルパーとして切り出してある。
    /// <see cref="InstallerBase"/>が骨格の一部として呼ぶ(#145).
    /// </remarks>
    internal static class DomainBuilder
    {
        private const string DefaultCommandAssembly = "Assembly-CSharp";

        /// <summary>
        /// ドメイン層のコンポーネント一式を構築する.
        /// </summary>
        internal static DomainContext Build(ITerminalOptions options)
        {
            var logger = new CommandLogger(options.BufferSize);
            var registry = new CommandRegistry(logger);
            var invoker = new CommandInvoker();
            var parser = new CommandParser();
            var history = new CommandHistory();
            var discover = new CommandDiscoverer(logger, new[] { DefaultCommandAssembly }.Concat(options.AdditionalCommandAssemblies ?? Array.Empty<string>()));
            var autocomplete = new CommandAutocomplete();
            var normalMode = new ExecutionMode(logger, registry, invoker, parser, history, autocomplete) { Prompt = options.Prompt };
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
                Mode = normalMode,
            };
        }

        /// <summary>
        /// 属性で発見したコマンドとパッケージ内蔵コマンドを登録する.
        /// </summary>
        internal static void RegisterCommands(in DomainContext domain)
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
            RegisterBuiltinCommands(in domain, in bundle);
        }

        private static void RegisterBuiltinCommands(in DomainContext domain, in ModeServiceBundle bundle)
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
    }
}

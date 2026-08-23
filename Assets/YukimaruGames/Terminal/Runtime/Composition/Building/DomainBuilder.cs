using System;
using System.Collections.Generic;
using System.Reflection;
using YukimaruGames.Terminal.Application.Services;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Repositories;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Repositories;
using YukimaruGames.Terminal.Domain.Services;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Infrastructure.Discoverer;
using YukimaruGames.Terminal.Infrastructure.Factories;
using YukimaruGames.Terminal.Infrastructure.Modes;
using YukimaruGames.Terminal.SharedKernel;

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
            var discover = new CommandDiscoverer(logger);
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

            // NOTE: 走査範囲がAssembly-CSharp限定ではなくなった(#176)ため、Editorプロセスでは
            // Test用asmdef(EditMode/PlayMode)に属する検証専用の不正な形状のメソッド
            // (非同期voidメソッド等、CommandFactoryTestsが意図的に用意しているもの)も
            // 発見対象に含まれうる。1件のCommandFactory.Create失敗が全体のInstall()を
            // 巻き添えで落とさないよう、ここで個別に捕捉してログに残し読み飛ばす.
            var specs = domain.Discoverer.Discover();
            foreach (var spec in specs)
            {
                CommandHandler handler;
                try
                {
                    handler = CommandFactory.Create(spec.Method, bundle);
                }
                catch (Exception e)
                {
                    domain.Logger?.Send(
                        MessageType.Warning,
                        $"Failed to create command handler for '{spec.Meta.Command}' " +
                        $"({spec.Method.DeclaringType?.FullName}.{spec.Method.Name}). Skipped.{Environment.NewLine}{e.GetType().Name}:{e.Message}");
                    continue;
                }

                if (domain.Registry.Add(spec.Meta.Command, handler))
                {
                    domain.Autocomplete.Register(spec.Meta.Command);
                }
            }

            // terminal.stack等のパッケージ内蔵コマンドには[TerminalCommand]属性を付与していない
            // (#176フォローアップ)。自動探索の走査範囲がAssembly-CSharp限定から拡張されたことで
            // Infrastructureアセンブリも対象に入るようになり、属性を付けたままだと明示登録との
            // 二重登録エラーになるため。リフレクション呼び出しコストの観点からも、ビルトイン
            // コマンドは自動探索を経由させず、ここで直接登録する.
            RegisterBuiltinCommands(in domain, in bundle);
        }

        private static void RegisterBuiltinCommands(in DomainContext domain, in ModeServiceBundle bundle)
        {
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinDiagnosticsCommands.Commands);
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinGeneralCommands.Commands);

#if UNITY_EDITOR
            // Editor限定コマンドは実機ビルド(UNITY_EDITOR未定義)では型ごとコンパイル対象外になる
            // ため、この呼び出し自体も#if UNITY_EDITORで囲い、実機ビルドに参照を残さない.
            RegisterBuiltinCommandMethods(domain, bundle, BuiltinEditorCommands.Commands);
#endif
        }

        private static void RegisterBuiltinCommandMethods(
            in DomainContext domain, in ModeServiceBundle bundle, (MethodInfo Method, CommandMeta Meta)[] commands)
        {
            foreach (var (method, meta) in commands)
            {
                var handler = CommandFactory.Create(null, method, meta, bundle);
                if (domain.Registry.Add(meta.Command, handler))
                {
                    domain.Autocomplete.Register(meta.Command);
                }
            }
        }
    }
}

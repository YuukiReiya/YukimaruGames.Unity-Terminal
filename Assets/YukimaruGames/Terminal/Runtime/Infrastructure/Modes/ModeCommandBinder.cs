using System;
using System.Collections.Generic;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Domain.Contracts.Modes.Null;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Infrastructure.Factories;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Infrastructure.Modes
{
    /// <summary>
    /// <see cref="IModeCommandBinder"/> の実装. リフレクション走査とExpression Tree生成という
    /// Infrastructure層の関心事を担う.
    /// </summary>
    /// <remarks>
    /// <c>Infrastructure</c>asmdefは<c>Domain.Services</c>を参照できない(レイヤ制約)ため、
    /// レジストリの具体型生成は <paramref name="registryFactory"/> として外部(Composition層)
    /// から注入してもらう.
    /// </remarks>
    public sealed class ModeCommandBinder : IModeCommandBinder
    {
        private readonly ICommandDiscoverer _discoverer;
        private readonly Func<ICommandRegistry> _registryFactory;
        private readonly ICommandLogger _logger;

        // (型, ModeId)の組ごとに1回だけコンパイルする(Expression.Constant(instance)の焼き込みが
        // できないため、インスタンス確定後の処理は Func<object, CommandHandler> に留める)。
        // キーにIdも含めるのは、[TerminalModeCommand(modeId: "...")]でのマッチングがIdに依存するため
        // (同一型でもインスタンスごとにIdが異なりうる設計を許容する。型ごとに不変ならキャッシュの
        // 実質的なサイズ・挙動は従来と変わらない).
        private readonly Dictionary<(Type Type, string Id), (string Command, Func<object, CommandHandler> Factory)[]> _compiled = new();

        public ModeCommandBinder(ICommandDiscoverer discoverer, Func<ICommandRegistry> registryFactory, ICommandLogger logger)
        {
            _discoverer = discoverer;
            _registryFactory = registryFactory;
            _logger = logger;
        }

        ICommandRegistry IModeCommandBinder.BindFor(ITerminalMode mode)
        {
            var type = mode.GetType();
            var key = (type, mode.Id ?? string.Empty);
            if (!_compiled.TryGetValue(key, out var entries))
            {
                entries = Compile(type, mode.Id);
                _compiled[key] = entries;
            }

            var registry = _registryFactory != null ? _registryFactory() : (ICommandRegistry)NullCommandRegistry.Instance;
            foreach (var (command, factory) in entries)
            {
                if (!registry.Add(command, factory(mode)))
                {
                    _logger?.Send(MessageType.Warning, $"Mode command '{command}' is already defined for '{type.FullName}'.");
                }
            }

            return registry;
        }

        private (string Command, Func<object, CommandHandler> Factory)[] Compile(Type modeType, string modeId)
        {
            var specs = _discoverer.DiscoverModeCommands(modeType, modeId);
            var result = new (string, Func<object, CommandHandler>)[specs.Count];
            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];
                result[i] = (spec.Meta.Command, CommandFactory.CreateBinder(spec.Method, spec.Meta, ModeServiceBundle.Empty));
            }

            return result;
        }
    }
}

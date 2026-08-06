using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Infrastructure.Discoverer
{
    /// <summary>
    /// コマンドを検出するためのクラス.
    /// </summary>
    public sealed class CommandDiscoverer : ICommandDiscoverer
    {
        private readonly ICommandLogger _logger;
        private readonly IEnumerable<string> _assemblyNames;

        // ReSharper disable once InconsistentNaming
        private const BindingFlags kBindingFlags =
            BindingFlags.Public | BindingFlags.Static |
            BindingFlags.InvokeMethod | BindingFlags.NonPublic;

        // ReSharper disable once InconsistentNaming
        private const BindingFlags kModeBindingFlags =
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public CommandDiscoverer(ICommandLogger logger)
            : this(logger, new[] { "Assembly-CSharp" })
        {
        }

        public CommandDiscoverer(ICommandLogger logger, IEnumerable<string> assemblyNames)
        {
            _logger = logger;
            _assemblyNames = (assemblyNames ?? Array.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        /// <inheritdoc/>
        public IEnumerable<CommandSpecification> Discover()
        {
            // 名前レベルの重複だけでなく、推移参照による実体レベルの重複も
            // ここで一元的に排除する(呼び出し元が重複を渡さない前提には依存しない).
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var specs = new List<CommandSpecification>();
            foreach (var name in _assemblyNames)
            {
                CollectInto(name, visited, specs);
            }

            return specs;
        }

        /// <summary>
        /// アセンブリ名からコマンドのハンドラーを検出.
        /// </summary>
        /// <param name="assemblyName">スキャン対象のAssembly名</param>
        /// <returns>取得した設計データを返す</returns>
        public IEnumerable<CommandSpecification> Discover(string assemblyName)
        {
            var specs = new List<CommandSpecification>();
            CollectInto(assemblyName, new HashSet<string>(StringComparer.Ordinal), specs);
            return specs;
        }

        private void CollectInto(string assemblyName, HashSet<string> visited, List<CommandSpecification> sink)
        {
            AssemblyName referencedAssemblyName = null;

            try
            {
                referencedAssemblyName = new AssemblyName(assemblyName);
                var assembly = Assembly.Load(assemblyName);
                if (assembly is null)
                {
                    _logger?.Send(MessageType.Error, $"Failed to load assembly: {assemblyName}. Assembly.Load returned null.");
                    return;
                }

                var toScan = new List<Assembly>();
                if (visited.Add(assembly.FullName))
                {
                    toScan.Add(assembly);
                }

                var referencedAssembliesNames = assembly.GetReferencedAssemblies();
                foreach (var name in referencedAssembliesNames)
                {
                    if (!visited.Add(name.FullName))
                    {
                        continue;
                    }

                    var referenced = Assembly.Load(name);
                    if (referenced != null)
                    {
                        toScan.Add(referenced);
                    }
                }

                foreach (var scanned in toScan)
                {
                    foreach (var type in GetTypesSafely(scanned))
                    {
                        foreach (var method in GetMethodsSafely(type, kBindingFlags))
                        {
                            if (!TryGetAttribute(method, out var attribute))
                            {
                                continue;
                            }

                            if (!IsDiscoverable(method, attribute))
                            {
                                continue;
                            }

                            sink.Add(new CommandSpecification(method, attribute.Meta));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Referenced assembly '{referencedAssemblyName}' from assembly '{assemblyName}' could not be loaded: {e.GetType()}{Environment.NewLine}{e.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<CommandSpecification> DiscoverModeCommands(Type modeType, string modeId)
        {
            var results = new List<CommandSpecification>();
            var seenOverrides = new HashSet<MethodInfo>();
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            for (var type = modeType; type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var method in GetMethodsSafely(type, kModeBindingFlags))
                {
                    var matched = GetModeAttributes(method).Where(a => Matches(a, modeType, modeId)).ToArray();
                    if (matched.Length == 0)
                    {
                        continue;
                    }

                    // overrideされたメソッドは基底定義で同一視し、派生側(先に列挙される)を優先する.
                    // (メソッド単位で1回だけ判定する: 属性ごとに判定すると AllowMultiple=true の
                    // 2個目以降の属性が誤って握り潰されるため.)
                    if (!seenOverrides.Add(method.GetBaseDefinition()))
                    {
                        continue;
                    }

                    foreach (var attribute in matched)
                    {
                        var commandName = attribute.Meta.Command;
                        if (string.IsNullOrWhiteSpace(commandName))
                        {
                            _logger?.Send(
                                MessageType.Warning,
                                $"Command name is null or empty for method '{method.Name}' in type '{method.DeclaringType!.FullName}'.");
                            continue;
                        }

                        if (!seenNames.Add(commandName))
                        {
                            _logger?.Send(
                                MessageType.Warning,
                                $"Mode command '{commandName}' is declared more than once in the hierarchy of '{modeType.FullName}'. The declaration in '{type.FullName}' is ignored.");
                            continue;
                        }

                        results.Add(new CommandSpecification(method, attribute.Meta));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// モード型・識別子に対して属性が適用可能かを判定する.
        /// </summary>
        private static bool Matches(TerminalModeCommandAttribute attribute, Type modeType, string modeId)
        {
            return attribute.ModeType != null
                ? attribute.ModeType.IsAssignableFrom(modeType)
                : string.Equals(attribute.ModeId, modeId, StringComparison.Ordinal);
        }

        /// <summary>
        /// [TerminalModeCommand] を複数形で取得する(AllowMultiple=trueのため).
        /// </summary>
        /// <remarks>
        /// [TerminalCommand](AllowMultiple=false)は単数形の <see cref="TryGetAttribute"/> を使う。
        /// 複数許容の属性を単数形で取得すると AmbiguousMatchException になるため使い分けに注意.
        /// </remarks>
        private IEnumerable<TerminalModeCommandAttribute> GetModeAttributes(MethodInfo methodInfo)
        {
            try
            {
                var attributes = Attribute.GetCustomAttributes(methodInfo, typeof(TerminalModeCommandAttribute), inherit: false);
                if (attributes.Length == 0)
                {
                    return Array.Empty<TerminalModeCommandAttribute>();
                }

                var result = new TerminalModeCommandAttribute[attributes.Length];
                Array.Copy(attributes, result, attributes.Length);
                return result;
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Warning,
                    $"Failed to read TerminalModeCommandAttribute(s) for method '{methodInfo.Name}' in type '{methodInfo.DeclaringType!.FullName}'.{Environment.NewLine}{e.GetType().Name}:{e.Message}");
                return Array.Empty<TerminalModeCommandAttribute>();
            }
        }

        /// <summary>
        /// Assemblyから有効な型を取得.
        /// </summary>
        private IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Failed to load some types from assembly '{assembly.FullName}'.{Environment.NewLine}Exception: {e}");
                return e.Types.Where(t => t != null);
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Unexpected error while getting types from assembly '{assembly.FullName}'.{Environment.NewLine}Exception: {e}");
                return Enumerable.Empty<Type>();
            }
        }

        /// <summary>
        /// Typeから安全にメソッドを取り出す.
        /// </summary>
        private IEnumerable<MethodInfo> GetMethodsSafely(Type type, BindingFlags bindingFlags)
        {
            try
            {
                return type.GetMethods(bindingFlags);
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Failed to load methods from type '{type.FullName}'.{Environment.NewLine}Exception: {e}");
                return Enumerable.Empty<MethodInfo>();
            }
        }

        /// <summary>
        /// アトリビュート取得の試行.
        /// </summary>
        private bool TryGetAttribute(MethodInfo methodInfo, out TerminalCommandAttribute attribute)
        {
            try
            {
                attribute = Attribute.GetCustomAttribute(methodInfo, typeof(TerminalCommandAttribute)) as TerminalCommandAttribute;
                return attribute is not null;
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Warning,
                    $"Command name is null or empty for method '{methodInfo.Name}' in type '{methodInfo.DeclaringType!.FullName}'.{Environment.NewLine}{e.GetType().Name}:{e.Message}{e.StackTrace}");
                attribute = null;
                return false;
            }
        }

        /// <summary>
        /// 発見可能か.
        /// </summary>
        private bool IsDiscoverable(MethodInfo methodInfo, TerminalCommandAttribute attribute)
        {
            // NOTE:
            // kBindingFlagsでInstanceメソッドを取りのぞいているので基本的には通らないはず.
            if (!methodInfo.IsStatic)
            {
                _logger?.Send(
                    MessageType.Warning,
                    $"Skipping non-static method '{methodInfo.Name}' in type '{methodInfo.DeclaringType!.FullName}'. Only static methods can be registered.");
                return false;
            }

            var commandName = attribute.Meta.Command;
            if (string.IsNullOrWhiteSpace(commandName))
            {
                _logger?.Send(
                    MessageType.Warning,
                    $"Command name is null or empty for method '{methodInfo.Name}' in type '{methodInfo.DeclaringType!.FullName}'.");
                return false;
            }

            return true;
        }
    }
}

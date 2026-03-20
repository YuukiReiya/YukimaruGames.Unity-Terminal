using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YukimaruGames.Terminal.Domain.Attribute;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Infrastructure.Discovery
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

        public CommandDiscoverer(ICommandLogger logger)
            : this(logger, new[] { "Assembly-CSharp" })
        {
        }

        public CommandDiscoverer(ICommandLogger logger, IEnumerable<string> assemblyNames)
        {
            _logger = logger;
            _assemblyNames = assemblyNames;
        }

        /// <inheritdoc/>
        public IEnumerable<CommandSpecification> Discover()
        {
            return _assemblyNames.SelectMany(Discover);
        }

        /// <summary>
        /// アセンブリ名からコマンドのハンドラーを検出.
        /// </summary>
        /// <param name="assemblyName">スキャン対象のAssembly名</param>
        /// <returns>取得した設計データを返す</returns>
        public IEnumerable<CommandSpecification> Discover(string assemblyName)
        {
            AssemblyName referencedAssemblyName = null;
            
            try
            {
                referencedAssemblyName = new AssemblyName(assemblyName);
                var assembly = Assembly.Load(assemblyName);
                if (assembly is null)
                {
                    _logger?.Send(MessageType.Error, $"Failed to load assembly: {assemblyName}. Assembly.Load returned null.");
                    return Enumerable.Empty<CommandSpecification>();
                }

                var dic = new Dictionary<AssemblyName, Assembly>(new[] { new KeyValuePair<AssemblyName, Assembly>(referencedAssemblyName, assembly) });
                var referencedAssembliesNames = assembly.GetReferencedAssemblies();
                foreach (var name in referencedAssembliesNames)
                {
                    if (!dic.TryGetValue(name, out assembly))
                    {
                        assembly = Assembly.Load(name);
                        if (assembly != null)
                        {
                            dic.Add(name, assembly);
                        }
                    }
                }

                var specs = new List<CommandSpecification>();
                foreach (var kvp  in dic)
                {
                    assembly = kvp.Value;
                    foreach (var type in GetTypesSafely(assembly))
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

                            specs.Add(new CommandSpecification(method, attribute.Meta));
                        }
                    }
                }

                return specs;
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Referenced assembly '{referencedAssemblyName}' from assembly '{assemblyName}' could not be loaded: {e.GetType()}{Environment.NewLine}{e.Message}");
                throw;
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
            // TODO:
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

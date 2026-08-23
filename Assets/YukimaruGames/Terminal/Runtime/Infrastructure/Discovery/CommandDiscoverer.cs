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

        // ReSharper disable once InconsistentNaming
        private const BindingFlags kBindingFlags =
            BindingFlags.Public | BindingFlags.Static |
            BindingFlags.InvokeMethod | BindingFlags.NonPublic;

        // ReSharper disable once InconsistentNaming
        private const BindingFlags kModeBindingFlags =
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private const string NUnitFrameworkAssemblyName = "nunit.framework";
        private const string UnityEngineTestRunnerAssemblyName = "UnityEngine.TestRunner";
        private const string UnityEditorTestRunnerAssemblyName = "UnityEditor.TestRunner";

        /// <summary>
        /// Unityのテストアセンブリ(EditMode/PlayMode)が(明示的な参照指定の有無に関わらず)
        /// 自動的に付与される参照名. これらを参照しているアセンブリはテスト専用と判断して
        /// 走査対象から除外する(#176フォローアップ: テスト用の検証専用メソッドが実際の
        /// ターミナルへ混入していた不具合の修正).
        /// </summary>
        private static readonly string[] _testAssemblyMarkerNames =
        {
            NUnitFrameworkAssemblyName,
            UnityEngineTestRunnerAssemblyName,
            UnityEditorTestRunnerAssemblyName,
        };

        public CommandDiscoverer(ICommandLogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 走査対象アセンブリは固定リストではなく、<see cref="TerminalCommandAttribute"/>の
        /// 定義アセンブリを直接・間接に参照している <see cref="AppDomain"/> 上の全アセンブリを
        /// 自動的に対象とする。これにより Assembly-CSharp 直下・独自asmdef配下のどちらに
        /// コマンドを置いても(属性を使う以上必ずこのアセンブリを参照するため)手動設定なしに
        /// 発見できる(#176)。ただしUnityのテストアセンブリ(EditMode/PlayMode)は
        /// <see cref="_testAssemblyMarkerNames"/>への参照を目印に除外する
        /// (テスト専用の検証用メソッドが実際のターミナルへ混入するのを防ぐため).
        /// </remarks>
        public IEnumerable<CommandSpecification> Discover()
        {
            var specs = new List<CommandSpecification>();

            foreach (var assembly in GetCandidateAssemblies())
            {
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

        /// <summary>
        /// <see cref="TerminalCommandAttribute"/>の定義アセンブリ自身、および
        /// それを直接参照しているアセンブリを走査対象として列挙する.
        /// </summary>
        private IEnumerable<Assembly> GetCandidateAssemblies()
        {
            var markerAssemblyName = typeof(TerminalCommandAttribute).Assembly.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var referencedNames = GetReferencedAssemblyNamesSafely(assembly);
                if (referencedNames is null)
                {
                    continue;
                }

                if (IsTestAssembly(referencedNames))
                {
                    continue;
                }

                if (string.Equals(assembly.GetName().Name, markerAssemblyName, StringComparison.Ordinal) ||
                    referencedNames.Contains(markerAssemblyName, StringComparer.Ordinal))
                {
                    yield return assembly;
                }
            }
        }

        private static bool IsTestAssembly(IEnumerable<string> referencedNames) =>
            referencedNames.Any(n => _testAssemblyMarkerNames.Contains(n, StringComparer.Ordinal));

        private string[] GetReferencedAssemblyNamesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetReferencedAssemblies().Select(n => n.Name).ToArray();
            }
            catch (Exception e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Failed to inspect references of assembly '{assembly.FullName}'.{Environment.NewLine}{e.GetType()}:{e.Message}");
                return null;
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

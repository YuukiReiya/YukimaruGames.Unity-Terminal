using System;
using System.Collections.Generic;
using System.Reflection;
using YukimaruGames.Terminal.Domain.Attribute;
using YukimaruGames.Terminal.Domain.Interface;
using YukimaruGames.Terminal.Domain.Model;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Service
{
    /// <summary>
    /// 登録されたコマンドの保存クラス.
    /// </summary>
    public sealed class CommandRegistry : ICommandRegistry
    {
        /// <summary>
        /// コマンドキャッシュ.
        /// </summary>
        private readonly Dictionary<string /* コマンドのエイリアス */, CommandHandler /* ハンドル */> _commands =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ログ発行.
        /// </summary>
        private readonly ICommandLogger _logger;
        
        /// <summary>
        /// メソッド登録のバインディングフラグ.
        /// </summary>
        private const BindingFlags BindingFlags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.CreateInstance |
            System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic;

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="logger">ロガーインスタンス.</param>
        public CommandRegistry(ICommandLogger logger) => _logger = logger;
        
        /// <summary>
        /// コマンドの追加.
        /// </summary>
        /// <param name="command">コマンド名</param>
        /// <param name="handle">コマンドのハンドル</param>
        public bool Add(string command, CommandHandler handle)
        {
            if (_commands.TryAdd(command, handle)) return true;
            _logger?.Send(MessageType.Error, $"Command '{command}' is already defined.");
            return false;
        }

        /// <summary>
        /// コマンドの追加.
        /// </summary>
        /// <param name="command">コマンド名</param>
        /// <param name="methodInfo">メソッド情報</param>
        /// <param name="attribute">属性</param>
        private void Add(string command, MethodInfo methodInfo, RegisterAttribute attribute)
        {
            var proc = (CommandDelegate)Delegate.CreateDelegate(typeof(CommandDelegate), methodInfo);
            Add(command, new CommandHandler(
                proc,
                command,
                attribute.Meta.MinArgCount,
                attribute.Meta.MaxArgCount,
                attribute.Meta.Help
            ));
        }

        /// <summary>
        /// コマンドの削除.
        /// </summary>
        /// <param name="command">コマンドのエイリアス</param>
        public bool Remove(string command)
        {
            if (_commands.ContainsKey(command))
            {
                _commands.Remove(command);
                return true;
            }

            _logger?.Send(MessageType.Error, $"Command '{command}' is not registered.");
            return false;
        }
        
        /// <inheritdoc/> 
        public bool TryGet(string command, out CommandHandler handler)
        {
            if (_commands.TryGetValue(command, out handler))
            {
                return true;
            }
            return false;
        } 

        /// <summary>
        /// Assemblyから利用できるメソッドを登録.
        /// </summary>
        /// <param name="assembly">対象Assembly</param>
        /// <param name="bindingFlags">バインディングフラグ</param>
        /// <remarks>
        /// Assemblyから参照させる場合はインスタンスを指定できない。
        /// (作られていないインスタンスを指定できるわけないので.)
        /// </remarks>
        private void Register(Assembly assembly, BindingFlags bindingFlags)
        {
            try
            {
                foreach (var type in GetTypesSafely(assembly))
                {
                    foreach (var method in GetMethodsSafely(type, bindingFlags))
                    {
                        Add(type, method);
                    }
                }
            }
            catch (System.Exception e)
            {
                _logger?.Send(
                    MessageType.Error,
                    $"Failed to register commands from assembly '{assembly.FullName}'.{Environment.NewLine}Exception: {e}");
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
                    MessageType.Warning,
                    $"Failed to load some types from assembly '{assembly.FullName}'.{Environment.NewLine}Exception: {e}");
                throw;
            }
            catch (System.Exception e)
            {
                _logger?.Send(
                    MessageType.Error,
                    $"Unexpected error while getting types from assembly '{assembly.FullName}'.{Environment.NewLine}Exception: {e}");
                throw;
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
            catch (System.Exception e)
            {
                _logger?.Send(
                    MessageType.Warning,
                    $"Failed to load methods from type '{type.FullName}'.{Environment.NewLine}Exception: {e}");
                throw;
            }
        }

        /// <summary>
        /// コマンドの追加.
        /// </summary>
        private void Add(Type declaringType, MethodInfo method)
        {
            try
            {
                if (System.Attribute.GetCustomAttribute(method, typeof(RegisterAttribute)) is not RegisterAttribute
                    attribute)
                {
                    // 属性が付与されていないメソッドはスキップ.
                    return;
                }

                if (!method.IsStatic)
                {
                    // Assemblyから登録するときは呼び出し元のインスタンスが特定できないはずなのでstaticのみ.
                    _logger?.Send(
                        MessageType.Warning,
                        $"Skipping non-static method '{method.Name}' in type '{declaringType.FullName}'. Only static methods can be registered.");
                    return;
                }
                
                var command = attribute.Meta.Command;
                if (string.IsNullOrEmpty(command) || string.IsNullOrWhiteSpace(command))
                {
                    _logger?.Send(
                        MessageType.Warning,
                        $"Command name is null or empty for method '{method.Name}' in type '{declaringType.FullName}'.");
                    return;
                }

                Add(command, method, attribute);
            }
            catch (System.Exception e)
            {
                _logger?.Send(
                    MessageType.Exception,
                    $"Exception: {e.GetType().Name}{Environment.NewLine}{e.Message}");
                throw;
            }
        }

        /// <summary>
        /// ランタイムでロード済みと未解決なAssemblyの中から利用できるメソッドを登録.
        /// </summary>
        /// <param name="unresolvedAssemblyNames">未解決(未ロード)のAssembly名</param>
        /// <param name="bindingFlags">登録メソッドのバインディングフラグ</param>
        /// <remarks>
        /// 注意: このメソッドを呼び出すことで未解決なAssemblyで有効な値のものはAssemblyがロードされます。
        /// <p>* 未解決のAssemblyの参照関係を自動で解決する、即ちそのAssemblyが参照しているAssemblyもロードされる。</p>
        /// <p>* 引数で渡されたAssembly名が既にロードされている場合も考慮。</p>
        /// <p>* Assembly名がnull/不正値なら登録はされない.</p>
        /// </remarks>
        public void Register(string[] unresolvedAssemblyNames, BindingFlags bindingFlags = BindingFlags)
        {
            if (unresolvedAssemblyNames is null) return;
            
            var hashSet = new HashSet<Assembly>();
            
            foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                hashSet.Add(loadedAssembly);
            }
            
            foreach (var unresolvedAssemblyName in unresolvedAssemblyNames)
            {
                AssemblyName referencedAssemblyName = null;
                try
                {
                    var assembly = Assembly.Load(unresolvedAssemblyName);
                    if (assembly is not null)
                    {
                        var referencedAssembliesName = assembly.GetReferencedAssemblies();
                        foreach (var assemblyName in referencedAssembliesName)
                        {
                            referencedAssemblyName = assemblyName;
                            var referencedAssembly = Assembly.Load(referencedAssemblyName);
                            if (hashSet.Add(referencedAssembly))
                            {
                                Register(referencedAssembly, bindingFlags);
                            }
                        }
                    }
                    else
                    {
                        _logger?.Send(
                            MessageType.Warning,
                            $"Failed to load assembly: {unresolvedAssemblyName}. Assembly.Load returned null.");
                    }
                }
                catch (System.Exception e)
                {
                    _logger?.Send(
                        MessageType.Error,
                        $"Referenced assembly '{referencedAssemblyName}' from assembly '{unresolvedAssemblyName}' could not be loaded.{Environment.NewLine}{e.GetType()}{Environment.NewLine}{e.Message}");
                    throw;
                }
            }
        }
    }
}

using System;
using YukimaruGames.Terminal.Domain.Service;

namespace YukimaruGames.Terminal.Runtime
{
    public sealed partial class TerminalBootstrapper
    {
        private const System.Reflection.BindingFlags BindingFlags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.CreateInstance |
            System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.SetField |
            System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic;

        /// <summary>
        /// メソッド名からメソッドが存在していればコマンドを登録.
        /// </summary>
        /// <param name="instance">登録するコマンドの呼び出しインスタンス</param>
        /// <param name="command">登録コマンド名</param>
        /// <param name="methodName">登録対象のメソッド名</param>
        /// <param name="supportsAutocomplete">自動補完文字列として登録するか</param>
        /// <typeparam name="T">インスタンス型(class)</typeparam>
        /// <remarks>
        /// <p>登録対象のメソッドが定義されていたらコマンド名で登録する</p>
        /// <p>TODO:</p>
        /// <p>※引数の数は厳格、そのためデフォルト引数は未サポート。</p>
        /// <p>用途:</p>
        /// <p>主に開発中での利用を想定。</p>
        /// <p>バージョン前後で宣言・定義の有無が変わるメソッドの登録などで有用。</p>
        /// <p>e.g.(本パッケージをローカルのみで利用する場合)</p>
        /// <p>feature ブランチの作業後に未マージの master ブランチへ移動。</p>
        /// <p>未マージのためメソッドの定義はされていないためコンパイルエラーが出てしまうなどの問題に対してのケア.</p>
        /// </remarks>
        /// <sample><code>
        /// void SomeMethod()
        /// {
        ///     // something
        /// }
        /// void Register()
        /// {
        ///     _bootstrapper.RegisterCommandIfMethodAvailable(this, "[コマンド名]", "SomeMethod");
        /// }
        /// </code></sample>
        public void RegisterCommandIfMethodAvailable<T>(T instance, string command, string methodName, bool supportsAutocomplete = true) where T : class
        {
            if (!TryGetMethodInfo(instance.GetType(), methodName, out var methodInfo))
            {
                _service.Error($"'{methodName}' is not found. so not command registered '{command}'");
                return;
            }
            
            var factory = new CommandFactory();
            var handler = factory.Create(instance, command, methodInfo);

            if (_registry.Add(command, handler)
                && supportsAutocomplete)
            {
                _autocomplete.Register(command);
            }
        }

        /// <summary>
        /// コマンドの登録.
        /// </summary>
        /// <param name="command">コマンド名</param>
        /// <param name="delegate">呼び出しデリゲート</param>
        /// <param name="supportsAutocomplete">自動補完文字列として登録するか</param>
        /// <typeparam name="TDelegate">デリゲート型</typeparam>
        /// <remarks>
        /// <p>Delegate型を利用することで引数の型は値型(CommandArgument型サポート)であれば不問</p>
        /// </remarks>
        /// <sample><code>
        /// void SomeMethod(int a,int b)
        /// {
        ///     // something
        /// }
        ///
        /// void Register()
        /// {
        ///     Action&lt;int,int&gt; @delegate = SomeMethod;
        ///     _bootstrapper.RegisterCommand("[コマンド名]", @delegate);
        /// }
        /// </code></sample>
        public void RegisterCommand<TDelegate>(string command, TDelegate @delegate, bool supportsAutocomplete = true) where TDelegate : Delegate
        {
            var factory = new CommandFactory();
            var handler = factory.Create(@delegate);

            if (_registry.Add(command, handler)
                && supportsAutocomplete)
            {
                _autocomplete.Register(command);
            }
        }

        /// <summary>
        /// メソッドの情報取り出し.
        /// </summary>
        /// <param name="type">対象型</param>
        /// <param name="methodName">対象メソッド</param>
        /// <param name="methodInfo">メソッド情報</param>
        /// <returns>
        /// <p>取得有無</p>
        /// <p>true : 成功</p>
        /// <p>false : 失敗</p>
        /// </returns>
        private bool TryGetMethodInfo(Type type, string methodName, out System.Reflection.MethodInfo methodInfo) =>
            (methodInfo = type.GetMethod(methodName, BindingFlags)) != null;
    }
}

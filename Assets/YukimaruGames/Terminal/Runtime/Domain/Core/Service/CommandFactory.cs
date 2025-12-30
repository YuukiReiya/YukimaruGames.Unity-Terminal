using System;
using System.Linq.Expressions;
using System.Reflection;
using YukimaruGames.Terminal.Domain.Attribute;
using YukimaruGames.Terminal.Domain.Exception;
using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Domain.Service
{
    /// <summary>
    /// 実行コマンドのFactory.
    /// </summary>
    public sealed class CommandFactory
    {
        /// <summary>
        /// コマンドハンドラーの生成.
        /// </summary>
        /// <param name="instance">インスタンス</param>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <param name="command">登録コマンド名</param>
        /// <param name="minArgCount">メソッドの最小引数</param>
        /// <param name="maxArgCount">メソッドの最大引数</param>
        /// <param name="help">ヘルプ</param>
        /// <returns>コマンドの実行型</returns>
        private CommandHandler Create(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help)
        {
            var parameter4Ex = Expression.Parameter(typeof(CommandArgument[]), "args");
            var methodParameters = methodInfo.GetParameters();
            Expression bodyEx;

            var isTakeRawArray = methodParameters.Length == 1 && methodParameters[0].ParameterType == typeof(CommandArgument[]);

            if (isTakeRawArray)
            {
                bodyEx = Expression.Call(null, methodInfo, parameter4Ex);
            }
            else
            {
                var methodCallExpression = BuildMethodCallExpression(instance, methodInfo, parameter4Ex, methodParameters);
                var validateCallExpression = BuildValidateExpression(parameter4Ex, minArgCount, maxArgCount);
                var throwException = Expression.Throw(
                    Expression.New(
                        typeof(CommandArgumentException).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(System.Exception) })!,
                        Expression.Property(parameter4Ex, "Length"),
                        Expression.Constant(minArgCount),
                        Expression.Constant(maxArgCount),
                        Expression.Constant(null, typeof(System.Exception))
                    ), typeof(void));

                bodyEx = Expression.Condition(validateCallExpression, methodCallExpression, throwException);
            }

            var lambda = Expression.Lambda<CommandDelegate>(bodyEx, parameter4Ex);
            var compiled = lambda.Compile();
            var meta = new CommandMeta(command, maxArgCount, minArgCount, help);
            return new CommandHandler(compiled, meta);
        }

        /// <summary>
        /// コマンドハンドラーの生成.
        /// </summary>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <returns>コマンドの実行型</returns>
        public CommandHandler Create(MethodInfo methodInfo)
        {
            var attribute = methodInfo.GetCustomAttribute<TerminalCommandAttribute>();
            var length = methodInfo.GetParameters().Length;
            return Create(
                null,
                methodInfo,
                attribute?.Meta.Command ?? string.Empty,
                attribute?.Meta.MinArgCount ?? length,
                attribute?.Meta.MaxArgCount ?? length,
                attribute?.Meta.Help ?? string.Empty);
        }

        /// <summary>
        /// コマンドハンドラーの生成.
        /// </summary>
        /// <param name="instance">呼び出しインスタンス</param>
        /// <param name="command">コマンド名</param>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <typeparam name="T">インスタンス型(class)</typeparam>
        /// <returns>コマンドの実行型</returns>
        /// <remarks>
        /// <p>TODO:</p>
        /// <p>オーバーロードされたメソッドの呼び出しをサポート出来ていない</p>
        /// </remarks>
        public CommandHandler Create<T>(T instance, string command, MethodInfo methodInfo) where T : class
        {
            var length = methodInfo.GetParameters().Length;
            return Create(
                instance,
                methodInfo,
                command,
                length,
                length,
                string.Empty);
        }

        /// <summary>
        /// コマンドハンドラーの作成
        /// </summary>
        /// <param name="delegate">デリゲート</param>
        /// <typeparam name="TDelegate">デリゲート型</typeparam>
        /// <returns>コマンドの実行型</returns>
        /// <sample><code>
        /// void SomeMethod()
        /// {
        ///     // something
        /// }
        ///
        /// void Register()
        /// {
        ///     Action @delegate = SomeMethod;
        ///     var handler = Create(@delegate);
        /// }
        /// </code></sample>
        public CommandHandler Create<TDelegate>(TDelegate @delegate) where TDelegate : Delegate
        {
            var methodInfo = @delegate.Method;
            var instance = @delegate.Target;
            var length = methodInfo.GetParameters().Length;
            return Create(
                instance,
                methodInfo,
                string.Empty,
                length,
                length,
                string.Empty);
        }
        
        /// <summary>
        /// メソッドの呼び出しExpressionを構築する.
        /// </summary>
        /// <param name="instance">メソッドの呼び出しインスタンス</param>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <param name="parameterExpression">呼び出し引数のExpression</param>
        /// <param name="methodParameters">メソッドに定義されている引数</param>
        /// <returns>構築したメソッドのExpression</returns>
        private Expression BuildMethodCallExpression(
            object instance,
            MethodInfo methodInfo,
            ParameterExpression parameterExpression,
            ParameterInfo[] methodParameters)
        {
            var instanceEx = methodInfo.IsStatic ? null : Expression.Constant(instance);
            var convertedArgEx = new Expression[methodParameters.Length];
            var mi2AsMethod = typeof(CommandArgument).GetMethod(nameof(CommandArgument.As));
            for (var i = 0; i < methodParameters.Length; i++)
            {
                var parameterInfo = methodParameters[i];
                var index4Ex = Expression.ArrayIndex(parameterExpression, Expression.Constant(i));
                var mi2AsGenericMethod = mi2AsMethod!.MakeGenericMethod(parameterInfo.ParameterType);
                var asGeneric4MethodEx = Expression.Call(index4Ex, mi2AsGenericMethod);

                // 実行時に引数型の変換を検知したら例外をthrowさせる.
                var catchBlock = Expression.Catch(
                    typeof(FormatException),
                    Expression.Throw(
                        Expression.New(
                            typeof(CommandFormatException).GetConstructor(new[] { typeof(int), typeof(string), typeof(Type), typeof(System.Exception) })!,
                            Expression.Constant(i),
                            Expression.Property(index4Ex, nameof(CommandArgument.String)),
                            Expression.Constant(parameterInfo.ParameterType, typeof(Type)),
                            Expression.Constant(null, typeof(System.Exception))
                        ), parameterInfo.ParameterType
                    )
                );

                convertedArgEx[i] = Expression.TryCatch(asGeneric4MethodEx, catchBlock);
            }

            return Expression.Call(instanceEx, methodInfo, convertedArgEx);
        }

        /// <summary>
        /// ValidateExpressionの構築.
        /// </summary>
        /// <param name="parameterExpression">呼び出し引数のExpression</param>
        /// <param name="minArgCount">メソッドの引数の最小数</param>
        /// <param name="maxArgCount">メソッドの引数の最大数</param>
        /// <returns>メソッドの呼び出し引数が閾値に収まっているか判定する条件式のExpression</returns>
        private Expression BuildValidateExpression(
            ParameterExpression parameterExpression,
            int minArgCount,
            int maxArgCount)
        {
            var argLength = Expression.Property(parameterExpression, "Length");
            var minCheck = Expression.GreaterThanOrEqual(argLength, Expression.Constant(minArgCount));
            var maxCheck = Expression.LessThanOrEqual(argLength, Expression.Constant(maxArgCount));
            return Expression.AndAlso(minCheck, maxCheck);    
        }
    }
}

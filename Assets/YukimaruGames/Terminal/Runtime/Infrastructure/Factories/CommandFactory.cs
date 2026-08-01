using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Abstractions.Attributes;
using YukimaruGames.Terminal.Domain.Abstractions.Exceptions;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;

namespace YukimaruGames.Terminal.Infrastructure.Factories
{
    /// <summary>
    /// 実行コマンドのFactory.
    /// </summary>
    public static class CommandFactory
    {
        /// <summary>
        /// 非同期コマンドメソッドの戻り値型の補正種別.
        /// </summary>
        internal enum AsyncReturnKind
        {
            /// <summary>
            /// ValueTaskを返すためラップ不要.
            /// </summary>
            None = 0,

            /// <summary>
            /// Taskを返すためValueTaskにラップが必要.
            /// </summary>
            TaskToValueTask = 1,
        }
        
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
        private static CommandHandler Create(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help)
        {
            var returnKind = GetReturnKind(methodInfo);

            return returnKind switch
            {
                CommandReturnKind.Sync => CreateSync(instance, methodInfo, command, minArgCount, maxArgCount, help),
                CommandReturnKind.AsyncTask => CreateAsync(instance, methodInfo, command, minArgCount, maxArgCount, help, returnKind: AsyncReturnKind.TaskToValueTask),
                CommandReturnKind.AsyncValueTask => CreateAsync(instance, methodInfo, command, minArgCount, maxArgCount, help, returnKind: AsyncReturnKind.None),
                _ => throw new NotSupportedException($"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' uses an unsupported return type.")
            };
        }

        /// <summary>
        /// 同期コマンドのハンドラーを生成する.
        /// </summary>
        private static CommandHandler CreateSync(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help)
        {
            if (HasTrailingCancellationToken(methodInfo.GetParameters()))
            {
                throw new NotSupportedException(
                    $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' uses CancellationToken. This factory path supports only sync commands without CancellationToken.");
            }

            var parameter4Ex = Expression.Parameter(typeof(ReadOnlyMemory<CommandArgument>), "args");
            var parameter4ArrayEx = Expression.Variable(typeof(CommandArgument[]), "argsArray");
            var methodParameters = methodInfo.GetParameters();
            Expression bodyEx;
            var toArrayMethod = typeof(ReadOnlyMemory<CommandArgument>).GetMethod(nameof(ReadOnlyMemory<CommandArgument>.ToArray))!;
            var convertToArrayEx = Expression.Assign(parameter4ArrayEx, Expression.Call(parameter4Ex, toArrayMethod));
            var instanceEx = methodInfo.IsStatic ? null : Expression.Constant(instance);

            var isTakeRawArray = methodParameters.Length == 1 && methodParameters[0].ParameterType == typeof(CommandArgument[]);
            var isTakeRawMemory = methodParameters.Length == 1 && methodParameters[0].ParameterType == typeof(ReadOnlyMemory<CommandArgument>);

            if (isTakeRawMemory)
            {
                bodyEx = Expression.Call(instanceEx, methodInfo, parameter4Ex);
            }
            else if (isTakeRawArray)
            {
                bodyEx = Expression.Block(
                    new[] { parameter4ArrayEx },
                    convertToArrayEx,
                    Expression.Call(instanceEx, methodInfo, parameter4ArrayEx));
            }
            else
            {
                var methodCallExpression = BuildMethodCallExpression(instance, methodInfo, parameter4ArrayEx, methodParameters);
                var validateCallExpression = BuildValidateExpression(parameter4ArrayEx, minArgCount, maxArgCount);
                var throwException = Expression.Throw(
                    Expression.New(
                        typeof(CommandArgumentException).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(Exception) })!,
                        Expression.Property(parameter4ArrayEx, "Length"),
                        Expression.Constant(minArgCount),
                        Expression.Constant(maxArgCount),
                        Expression.Constant(null, typeof(Exception))
                    ), typeof(void));

                bodyEx = Expression.Block(
                    new[] { parameter4ArrayEx },
                    convertToArrayEx,
                    Expression.Condition(validateCallExpression, methodCallExpression, throwException));
            }

            var lambda = Expression.Lambda<CommandDelegate>(bodyEx, parameter4Ex);
            var compiled = lambda.Compile();
            var meta = new CommandMeta(command, maxArgCount, minArgCount, help);
            return new CommandHandler(compiled, meta);
        }

        /// <summary>
        /// 非同期コマンドのハンドラーを生成する.
        /// </summary>
        private static CommandHandler CreateAsync(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help, AsyncReturnKind returnKind)
        {
            var parameter4Ex = Expression.Parameter(typeof(ReadOnlyMemory<CommandArgument>), "args");
            var cancellationTokenEx = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var parameter4ArrayEx = Expression.Variable(typeof(CommandArgument[]), "argsArray");
            var methodParameters = methodInfo.GetParameters();
            var instanceEx = methodInfo.IsStatic ? null : Expression.Constant(instance);
            var toArrayMethod = typeof(ReadOnlyMemory<CommandArgument>).GetMethod(nameof(ReadOnlyMemory<CommandArgument>.ToArray))!;
            var convertToArrayEx = Expression.Assign(parameter4ArrayEx, Expression.Call(parameter4Ex, toArrayMethod));
            var useCancellationToken = HasTrailingCancellationToken(methodParameters);
            var expectedLength = useCancellationToken ? 2 : 1;
            var useArrayPath = methodParameters.Length == expectedLength
                && methodParameters[0].ParameterType == typeof(CommandArgument[])
                && (!useCancellationToken || methodParameters[1].ParameterType == typeof(CancellationToken));
            var useMemoryPath = methodParameters.Length == expectedLength
                && methodParameters[0].ParameterType == typeof(ReadOnlyMemory<CommandArgument>)
                && (!useCancellationToken || methodParameters[1].ParameterType == typeof(CancellationToken));
            Expression bodyEx;

            if (useMemoryPath)
            {
                bodyEx = BuildAsyncReturnExpression(
                    Expression.Call(instanceEx, methodInfo, BuildAsyncCallArguments(parameter4Ex, cancellationTokenEx, useCancellationToken)),
                    returnKind);
            }
            else if (useArrayPath)
            {
                bodyEx = Expression.Block(
                    new[] { parameter4ArrayEx },
                    convertToArrayEx,
                    BuildAsyncReturnExpression(
                        Expression.Call(instanceEx, methodInfo, BuildAsyncCallArguments(parameter4ArrayEx, cancellationTokenEx, useCancellationToken)),
                        returnKind));
            }
            else
            {
                var methodCallExpression = BuildMethodCallExpression(instance, methodInfo, parameter4ArrayEx, methodParameters, useCancellationToken, cancellationTokenEx);
                if (returnKind is AsyncReturnKind.TaskToValueTask)
                {
                    methodCallExpression = BuildAsyncReturnExpression(methodCallExpression, returnKind);
                }

                var validateCallExpression = BuildValidateExpression(parameter4ArrayEx, minArgCount, maxArgCount);
                var throwException = Expression.Throw(
                    Expression.New(
                        typeof(CommandArgumentException).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(Exception) })!,
                        Expression.Property(parameter4ArrayEx, "Length"),
                        Expression.Constant(minArgCount),
                        Expression.Constant(maxArgCount),
                        Expression.Constant(null, typeof(Exception))
                    ), typeof(ValueTask));

                bodyEx = Expression.Block(
                    new[] { parameter4ArrayEx },
                    convertToArrayEx,
                    Expression.Condition(validateCallExpression, methodCallExpression, throwException));
            }

            var lambda = Expression.Lambda<CommandAsyncDelegate>(bodyEx, parameter4Ex, cancellationTokenEx);
            var compiled = lambda.Compile();
            var meta = new CommandMeta(command, maxArgCount, minArgCount, help);
            return new CommandHandler(compiled, meta);
        }

        /// <summary>
        /// コマンドの戻り値種別.
        /// </summary>
        private enum CommandReturnKind
        {
            Sync,
            AsyncTask,
            AsyncValueTask
        }

        /// <summary>
        /// メソッドの戻り値から処理系を判定する.
        /// </summary>
        private static CommandReturnKind GetReturnKind(MethodInfo methodInfo)
        {
            var returnType = methodInfo.ReturnType;
            if (returnType == typeof(void))
            {
                if (methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                {
                    throw new NotSupportedException(
                        $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' is async-void. Use the async factory path for async commands.");
                }

                return CommandReturnKind.Sync;
            }

            if (returnType == typeof(Task))
            {
                return CommandReturnKind.AsyncTask;
            }

            if (returnType == typeof(ValueTask))
            {
                return CommandReturnKind.AsyncValueTask;
            }

            if (returnType.IsGenericType)
            {
                var genericType = returnType.GetGenericTypeDefinition();
                if (genericType == typeof(Task<>) || genericType == typeof(ValueTask<>))
                {
                    throw new NotSupportedException(
                        $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' returns '{returnType.Name}'. Generic task return types are not supported.");
                }
            }

            throw new NotSupportedException(
                $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' returns '{returnType.Name}'. Only void, Task, and ValueTask are supported.");
        }

        /// <summary>
        /// コマンドハンドラーの生成.
        /// </summary>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <returns>コマンドの実行型</returns>
        public static CommandHandler Create(MethodInfo methodInfo)
        {
            var attribute = methodInfo.GetCustomAttribute<TerminalCommandAttribute>();
            var length = GetCommandArgumentCount(methodInfo);
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
        public static CommandHandler Create<T>(T instance, string command, MethodInfo methodInfo) where T : class
        {
            var length = GetCommandArgumentCount(methodInfo);
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
        public static CommandHandler Create<TDelegate>(TDelegate @delegate) where TDelegate : Delegate
        {
            var methodInfo = @delegate.Method;
            var instance = @delegate.Target;
            var length = GetCommandArgumentCount(methodInfo);
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
        private static Expression BuildMethodCallExpression(
            object instance,
            MethodInfo methodInfo,
            ParameterExpression parameterExpression,
            ParameterInfo[] methodParameters,
            bool useCancellationToken = false,
            ParameterExpression cancellationTokenExpression = null)
        {
            var instanceEx = methodInfo.IsStatic ? null : Expression.Constant(instance);
            var effectiveMethodParameters = GetEffectiveMethodParameters(methodParameters, useCancellationToken);
            var convertedArgEx = new Expression[effectiveMethodParameters.Length + (useCancellationToken ? 1 : 0)];
            var mi2AsMethod = typeof(CommandArgument).GetMethod(nameof(CommandArgument.As));
            for (var i = 0; i < effectiveMethodParameters.Length; i++)
            {
                var parameterInfo = effectiveMethodParameters[i];
                var index4Ex = Expression.ArrayIndex(parameterExpression, Expression.Constant(i));
                var mi2AsGenericMethod = mi2AsMethod!.MakeGenericMethod(parameterInfo.ParameterType);
                var asGeneric4MethodEx = Expression.Call(index4Ex, mi2AsGenericMethod);

                // 実行時に引数型の変換を検知したら例外をthrowさせる.
                var catchBlock = Expression.Catch(
                    typeof(FormatException),
                    Expression.Throw(
                        Expression.New(
                            typeof(CommandFormatException).GetConstructor(new[] { typeof(int), typeof(string), typeof(Type), typeof(Exception) })!,
                            Expression.Constant(i),
                            Expression.Property(index4Ex, nameof(CommandArgument.String)),
                            Expression.Constant(parameterInfo.ParameterType, typeof(Type)),
                            Expression.Constant(null, typeof(Exception))
                        ), parameterInfo.ParameterType
                    )
                );

                convertedArgEx[i] = Expression.TryCatch(asGeneric4MethodEx, catchBlock);
            }

            if (useCancellationToken)
            {
                convertedArgEx[convertedArgEx.Length - 1] = cancellationTokenExpression;
            }

            return Expression.Call(instanceEx, methodInfo, convertedArgEx);
        }

        /// <summary>
        /// 非同期コマンドの呼び出し引数を構築する.
        /// </summary>
        private static Expression[] BuildAsyncCallArguments(ParameterExpression argsExpression, ParameterExpression cancellationTokenExpression, bool useCancellationToken)
        {
            return useCancellationToken
                ? new Expression[] { argsExpression, cancellationTokenExpression }
                : new Expression[] { argsExpression };
        }

        /// <summary>
        /// Task 系の戻り値を ValueTask に正規化する.
        /// </summary>
        private static Expression BuildAsyncReturnExpression(Expression expression, AsyncReturnKind returnKind)
        {
            if (returnKind is AsyncReturnKind.None)
            {
                return expression;
            }

            var valueTaskCtor = typeof(ValueTask).GetConstructor(new[] { typeof(Task) })!;
            return Expression.New(valueTaskCtor, expression);
        }

        /// <summary>
        /// メソッド末尾の CancellationToken がコマンド引数ではないかを判定する.
        /// </summary>
        private static bool HasTrailingCancellationToken(ParameterInfo[] methodParameters)
        {
            return methodParameters.Length > 0 && methodParameters[^1].ParameterType == typeof(CancellationToken);
        }

        /// <summary>
        /// コマンド引数に該当するパラメータ群を取得する.
        /// </summary>
        private static ParameterInfo[] GetEffectiveMethodParameters(ParameterInfo[] methodParameters, bool useCancellationToken)
        {
            if (!useCancellationToken)
            {
                return methodParameters;
            }

            var size = methodParameters.Length - 1;
            var effectiveParameters = new ParameterInfo[size];
            Array.Copy(methodParameters, effectiveParameters, size);
            return effectiveParameters;
        }

        /// <summary>
        /// デフォルト引数数の算出.
        /// </summary>
        private static int GetCommandArgumentCount(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            return HasTrailingCancellationToken(parameters) ? parameters.Length - 1 : parameters.Length;
        }

        /// <summary>
        /// ValidateExpressionの構築.
        /// </summary>
        /// <param name="parameterExpression">呼び出し引数のExpression</param>
        /// <param name="minArgCount">メソッドの引数の最小数</param>
        /// <param name="maxArgCount">メソッドの引数の最大数</param>
        /// <returns>メソッドの呼び出し引数が閾値に収まっているか判定する条件式のExpression</returns>
        private static Expression BuildValidateExpression(
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

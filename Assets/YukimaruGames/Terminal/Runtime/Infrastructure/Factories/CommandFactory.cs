using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Domain.Contracts.Exceptions;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

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
        /// コマンドの戻り値種別.
        /// </summary>
        private enum CommandReturnKind
        {
            Sync,
            AsyncTask,
            AsyncValueTask
        }

        /// <summary>
        /// メソッドパラメータ1個が担う役割.
        /// </summary>
        private enum ParameterRole : byte
        {
            /// <summary>
            /// ターミナルコマンドの引数(<see cref="CommandArgument"/>由来)として扱う.
            /// </summary>
            CommandArgument = 0,

            /// <summary>
            /// 実行キャンセル用の <see cref="CancellationToken"/> として扱う.
            /// </summary>
            CancellationToken = 1,

            /// <summary>
            /// <see cref="ModeServiceBundle"/> から解決される、起動時に確定済みのサービスとして扱う.
            /// </summary>
            InjectedService = 2,
        }

        /// <summary>
        /// メソッドパラメータ1個の分類結果.
        /// </summary>
        private readonly struct ParameterSlot
        {
            public readonly ParameterRole Role;
            public readonly Type ParameterType;

            /// <summary>
            /// <see cref="Role"/> が <see cref="ParameterRole.CommandArgument"/> のときのみ有効な、
            /// args 配列上の位置.
            /// </summary>
            public readonly int ArgumentIndex;

            /// <summary>
            /// <see cref="Role"/> が <see cref="ParameterRole.InjectedService"/> のときのみ有効な、
            /// 式木へ焼き込む実体.
            /// </summary>
            public readonly object InjectedValue;

            public ParameterSlot(ParameterRole role, Type parameterType, int argumentIndex, object injectedValue)
            {
                Role = role;
                ParameterType = parameterType;
                ArgumentIndex = argumentIndex;
                InjectedValue = injectedValue;
            }
        }

        /// <summary>
        /// メソッド全パラメータの分類結果. 「末尾のCancellationToken 1個だけ特別扱い」という
        /// 従来の暗黙の前提を廃し、任意の位置・任意個数の特別扱いパラメータを表現する.
        /// </summary>
        private readonly struct ParameterPlan
        {
            public readonly ParameterSlot[] Slots;
            public readonly int CommandArgumentCount;
            public readonly bool UsesCancellationToken;

            /// <summary>
            /// コマンド引数に該当するパラメータが1個だけで、かつその型が <see cref="CommandArgument"/> の配列である.
            /// </summary>
            public readonly bool IsRawArray;

            /// <summary>
            /// コマンド引数に該当するパラメータが1個だけで、かつその型が <see cref="ReadOnlyMemory{T}"/> である.
            /// </summary>
            public readonly bool IsRawMemory;

            private ParameterPlan(ParameterSlot[] slots, int commandArgumentCount, bool usesCancellationToken, bool isRawArray, bool isRawMemory)
            {
                Slots = slots;
                CommandArgumentCount = commandArgumentCount;
                UsesCancellationToken = usesCancellationToken;
                IsRawArray = isRawArray;
                IsRawMemory = isRawMemory;
            }

            /// <summary>
            /// メソッド情報とサービスバンドルから分類結果を構築する.
            /// </summary>
            /// <remarks>
            /// 分類の優先順位は「<see cref="CancellationToken"/> &gt; <paramref name="services"/>による解決
            /// &gt; コマンド引数」の順で固定。<paramref name="services"/>に<c>string</c>や<c>int</c>のような
            /// 基本型を登録すると、本来コマンド引数であるはずのパラメータが黙ってサービス解決に
            /// 差し替わる点に注意(現状の配線ではインターフェイス型のみを登録するため実害は無い)。
            /// </remarks>
            public static ParameterPlan Build(MethodInfo methodInfo, in ModeServiceBundle services)
            {
                var parameters = methodInfo.GetParameters();
                var slots = new ParameterSlot[parameters.Length];
                var argumentIndex = 0;
                var usesCancellationToken = false;
                Type rawArgumentType = null;

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;

                    if (parameterType == typeof(CancellationToken))
                    {
                        if (usesCancellationToken)
                        {
                            throw new NotSupportedException(
                                $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' declares CancellationToken more than once.");
                        }

                        usesCancellationToken = true;
                        slots[i] = new ParameterSlot(ParameterRole.CancellationToken, parameterType, -1, null);
                        continue;
                    }

                    if (services.TryResolve(parameterType, out var injectedValue))
                    {
                        slots[i] = new ParameterSlot(ParameterRole.InjectedService, parameterType, -1, injectedValue);
                        continue;
                    }

                    if (parameterType == typeof(CommandArgument[]) || parameterType == typeof(ReadOnlyMemory<CommandArgument>))
                    {
                        rawArgumentType = parameterType;
                    }

                    slots[i] = new ParameterSlot(ParameterRole.CommandArgument, parameterType, argumentIndex, null);
                    argumentIndex++;
                }

                var isRawArray = argumentIndex == 1 && rawArgumentType == typeof(CommandArgument[]);
                var isRawMemory = argumentIndex == 1 && rawArgumentType == typeof(ReadOnlyMemory<CommandArgument>);

                return new ParameterPlan(slots, argumentIndex, usesCancellationToken, isRawArray, isRawMemory);
            }
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
        /// <param name="services">注入可能なサービス群</param>
        /// <returns>コマンドの実行型</returns>
        private static CommandHandler Create(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help, in ModeServiceBundle services)
        {
            var returnKind = GetReturnKind(methodInfo);

            return returnKind switch
            {
                CommandReturnKind.Sync => CreateSync(instance, methodInfo, command, minArgCount, maxArgCount, help, services),
                CommandReturnKind.AsyncTask => CreateAsync(instance, methodInfo, command, minArgCount, maxArgCount, help, returnKind: AsyncReturnKind.TaskToValueTask, services),
                CommandReturnKind.AsyncValueTask => CreateAsync(instance, methodInfo, command, minArgCount, maxArgCount, help, returnKind: AsyncReturnKind.None, services),
                _ => throw new NotSupportedException($"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' uses an unsupported return type.")
            };
        }

        /// <summary>
        /// 同期コマンドのハンドラーを生成する.
        /// </summary>
        private static CommandHandler CreateSync(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help, in ModeServiceBundle services)
        {
            var plan = ParameterPlan.Build(methodInfo, services);
            ValidateSyncPlan(methodInfo, plan);

            var memoryEx = Expression.Parameter(typeof(ReadOnlyMemory<CommandArgument>), "args");
            var arrayEx = Expression.Variable(typeof(CommandArgument[]), "argsArray");
            var instanceEx = methodInfo.IsStatic ? null : Expression.Constant(instance);
            var bodyEx = BuildSyncBody(instanceEx, methodInfo, plan, memoryEx, arrayEx, minArgCount, maxArgCount);

            var lambda = Expression.Lambda<CommandDelegate>(bodyEx, memoryEx);
            var compiled = lambda.Compile();
            var meta = new CommandMeta(command, maxArgCount, minArgCount, help);
            return new CommandHandler(compiled, meta);
        }

        /// <summary>
        /// 非同期コマンドのハンドラーを生成する.
        /// </summary>
        private static CommandHandler CreateAsync(object instance, MethodInfo methodInfo, string command, int minArgCount, int maxArgCount, string help, AsyncReturnKind returnKind, in ModeServiceBundle services)
        {
            var plan = ParameterPlan.Build(methodInfo, services);

            var memoryEx = Expression.Parameter(typeof(ReadOnlyMemory<CommandArgument>), "args");
            var cancellationTokenEx = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
            var arrayEx = Expression.Variable(typeof(CommandArgument[]), "argsArray");
            var instanceEx = methodInfo.IsStatic ? null : Expression.Constant(instance);
            var bodyEx = BuildAsyncBody(instanceEx, methodInfo, plan, memoryEx, arrayEx, cancellationTokenEx, minArgCount, maxArgCount, returnKind);

            var lambda = Expression.Lambda<CommandAsyncDelegate>(bodyEx, memoryEx, cancellationTokenEx);
            var compiled = lambda.Compile();
            var meta = new CommandMeta(command, maxArgCount, minArgCount, help);
            return new CommandHandler(compiled, meta);
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
        public static CommandHandler Create(MethodInfo methodInfo) => Create(methodInfo, ModeServiceBundle.Empty);

        /// <summary>
        /// コマンドハンドラーの生成(サービス注入対応).
        /// </summary>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <param name="services">注入可能なサービス群</param>
        /// <returns>コマンドの実行型</returns>
        public static CommandHandler Create(MethodInfo methodInfo, in ModeServiceBundle services)
        {
            var attribute = methodInfo.GetCustomAttribute<TerminalCommandAttribute>();
            var length = GetCommandArgumentCount(methodInfo, services);
            return Create(
                null,
                methodInfo,
                attribute?.Meta.Command ?? string.Empty,
                attribute?.Meta.MinArgCount ?? length,
                attribute?.Meta.MaxArgCount ?? length,
                attribute?.Meta.Help ?? string.Empty,
                services);
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
            var length = GetCommandArgumentCount(methodInfo, ModeServiceBundle.Empty);
            return Create(
                instance,
                methodInfo,
                command,
                length,
                length,
                string.Empty,
                ModeServiceBundle.Empty);
        }

        /// <summary>
        /// コマンドハンドラーの生成(メタ情報・サービス注入対応).
        /// </summary>
        /// <param name="instance">呼び出しインスタンス</param>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <param name="meta">メタ情報(min/max/helpを保持する)</param>
        /// <param name="services">注入可能なサービス群</param>
        /// <returns>コマンドの実行型</returns>
        public static CommandHandler Create(object instance, MethodInfo methodInfo, in CommandMeta meta, in ModeServiceBundle services)
        {
            return Create(instance, methodInfo, meta.Command, meta.MinArgCount, meta.MaxArgCount, meta.Help, services);
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
            var length = GetCommandArgumentCount(methodInfo, ModeServiceBundle.Empty);
            return Create(
                instance,
                methodInfo,
                string.Empty,
                length,
                length,
                string.Empty,
                ModeServiceBundle.Empty);
        }

        /// <summary>
        /// インスタンスメソッドを対象に、型単位で1回だけ式木をコンパイルし、
        /// インスタンスごとのハンドラー生成だけを行う軽量なバインダーを返す.
        /// </summary>
        /// <remarks>
        /// <see cref="Create(object,MethodInfo,in CommandMeta,in ModeServiceBundle)"/> はインスタンスを
        /// <c>Expression.Constant</c> として式木に焼き込むため、モードへの入場ごとに再コンパイルが発生する。
        /// 同一モード型への入場を繰り返す場合、このメソッドで型単位のコンパイル結果を再利用できる。
        /// </remarks>
        /// <param name="methodInfo">呼び出しメソッド情報(インスタンスメソッドのみ)</param>
        /// <param name="meta">メタ情報</param>
        /// <param name="services">注入可能なサービス群</param>
        /// <returns>インスタンスを受け取り <see cref="CommandHandler"/> を生成する関数</returns>
        public static Func<object, CommandHandler> CreateBinder(MethodInfo methodInfo, in CommandMeta meta, in ModeServiceBundle services)
        {
            if (methodInfo.IsStatic)
            {
                throw new NotSupportedException(
                    $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' is static. Use Create(MethodInfo, in ModeServiceBundle) for static commands.");
            }

            var returnKind = GetReturnKind(methodInfo);
            var plan = ParameterPlan.Build(methodInfo, services);
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var instanceEx = (Expression)Expression.Convert(instanceParam, methodInfo.DeclaringType!);
            var memoryEx = Expression.Parameter(typeof(ReadOnlyMemory<CommandArgument>), "args");
            var arrayEx = Expression.Variable(typeof(CommandArgument[]), "argsArray");
            var meta4Closure = meta;

            switch (returnKind)
            {
                case CommandReturnKind.Sync:
                {
                    ValidateSyncPlan(methodInfo, plan);
                    var bodyEx = BuildSyncBody(instanceEx, methodInfo, plan, memoryEx, arrayEx, meta.MinArgCount, meta.MaxArgCount);
                    var innerLambda = Expression.Lambda<CommandDelegate>(bodyEx, memoryEx);
                    var outerLambda = Expression.Lambda<Func<object, CommandDelegate>>(innerLambda, instanceParam);
                    var factory = outerLambda.Compile();
                    return obj => new CommandHandler(factory(obj), meta4Closure);
                }

                case CommandReturnKind.AsyncTask:
                case CommandReturnKind.AsyncValueTask:
                {
                    var asyncReturnKind = returnKind == CommandReturnKind.AsyncTask ? AsyncReturnKind.TaskToValueTask : AsyncReturnKind.None;
                    var cancellationTokenEx = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                    var bodyEx = BuildAsyncBody(instanceEx, methodInfo, plan, memoryEx, arrayEx, cancellationTokenEx, meta.MinArgCount, meta.MaxArgCount, asyncReturnKind);
                    var innerLambda = Expression.Lambda<CommandAsyncDelegate>(bodyEx, memoryEx, cancellationTokenEx);
                    var outerLambda = Expression.Lambda<Func<object, CommandAsyncDelegate>>(innerLambda, instanceParam);
                    var factory = outerLambda.Compile();
                    return obj => new CommandHandler(factory(obj), meta4Closure);
                }

                default:
                    throw new NotSupportedException($"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' uses an unsupported return type.");
            }
        }

        /// <summary>
        /// 同期コマンドが CancellationToken を利用していないかを検証する.
        /// </summary>
        private static void ValidateSyncPlan(MethodInfo methodInfo, in ParameterPlan plan)
        {
            if (plan.UsesCancellationToken)
            {
                throw new NotSupportedException(
                    $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' uses CancellationToken. This factory path supports only sync commands without CancellationToken.");
            }
        }

        /// <summary>
        /// 同期コマンドの本体Expressionを構築する.
        /// </summary>
        private static Expression BuildSyncBody(Expression instanceEx, MethodInfo methodInfo, in ParameterPlan plan, ParameterExpression memoryEx, ParameterExpression arrayEx, int minArgCount, int maxArgCount)
        {
            if (plan.IsRawMemory)
            {
                return BuildCallExpression(instanceEx, methodInfo, plan, memoryEx, arrayEx, null);
            }

            if (plan.IsRawArray)
            {
                var callEx = BuildCallExpression(instanceEx, methodInfo, plan, memoryEx, arrayEx, null);
                return Expression.Block(
                    new[] { arrayEx },
                    BuildArgsToArrayAssign(memoryEx, arrayEx),
                    callEx);
            }

            {
                var callEx = BuildCallExpression(instanceEx, methodInfo, plan, memoryEx, arrayEx, null);
                var validateCallExpression = BuildValidateExpression(arrayEx, minArgCount, maxArgCount);
                var throwException = BuildArgumentCountThrow(arrayEx, minArgCount, maxArgCount, typeof(void));

                return Expression.Block(
                    new[] { arrayEx },
                    BuildArgsToArrayAssign(memoryEx, arrayEx),
                    Expression.Condition(validateCallExpression, callEx, throwException));
            }
        }

        /// <summary>
        /// 非同期コマンドの本体Expressionを構築する.
        /// </summary>
        private static Expression BuildAsyncBody(Expression instanceEx, MethodInfo methodInfo, in ParameterPlan plan, ParameterExpression memoryEx, ParameterExpression arrayEx, ParameterExpression cancellationTokenEx, int minArgCount, int maxArgCount, AsyncReturnKind returnKind)
        {
            if (plan.IsRawMemory)
            {
                var callEx = BuildCallExpression(instanceEx, methodInfo, plan, memoryEx, arrayEx, cancellationTokenEx);
                return BuildAsyncReturnExpression(callEx, returnKind);
            }

            if (plan.IsRawArray)
            {
                var callEx = BuildCallExpression(instanceEx, methodInfo, plan, memoryEx, arrayEx, cancellationTokenEx);
                return Expression.Block(
                    new[] { arrayEx },
                    BuildArgsToArrayAssign(memoryEx, arrayEx),
                    BuildAsyncReturnExpression(callEx, returnKind));
            }

            {
                var callEx = BuildCallExpression(instanceEx, methodInfo, plan, memoryEx, arrayEx, cancellationTokenEx);
                if (returnKind is AsyncReturnKind.TaskToValueTask)
                {
                    callEx = BuildAsyncReturnExpression(callEx, returnKind);
                }

                var validateCallExpression = BuildValidateExpression(arrayEx, minArgCount, maxArgCount);
                var throwException = BuildArgumentCountThrow(arrayEx, minArgCount, maxArgCount, typeof(ValueTask));

                return Expression.Block(
                    new[] { arrayEx },
                    BuildArgsToArrayAssign(memoryEx, arrayEx),
                    Expression.Condition(validateCallExpression, callEx, throwException));
            }
        }

        /// <summary>
        /// メソッド呼び出しExpressionを構築する.
        /// </summary>
        /// <param name="instanceEx">呼び出しインスタンスを表すExpression(staticメソッドならnull)</param>
        /// <param name="methodInfo">呼び出しメソッド情報</param>
        /// <param name="plan">パラメータの分類結果</param>
        /// <param name="memoryEx"><see cref="ReadOnlyMemory{CommandArgument}"/> 引数のExpression</param>
        /// <param name="arrayEx">配列変換後の変数Expression(raw memory以外で使用)</param>
        /// <param name="cancellationTokenExpression">CancellationToken引数のExpression(同期パスではnull)</param>
        /// <returns>構築したメソッド呼び出しのExpression</returns>
        private static Expression BuildCallExpression(
            Expression instanceEx,
            MethodInfo methodInfo,
            in ParameterPlan plan,
            ParameterExpression memoryEx,
            ParameterExpression arrayEx,
            ParameterExpression cancellationTokenExpression)
        {
            var asMethod = typeof(CommandArgument).GetMethod(nameof(CommandArgument.As));
            var callArgs = new Expression[plan.Slots.Length];

            for (var i = 0; i < plan.Slots.Length; i++)
            {
                var slot = plan.Slots[i];
                switch (slot.Role)
                {
                    case ParameterRole.CancellationToken:
                        if (cancellationTokenExpression is null)
                        {
                            // CreateSync/CreateBinder側で事前に弾いているため、ここへは通常到達しない防御的分岐.
                            throw new NotSupportedException(
                                $"Method '{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}' uses CancellationToken. This factory path supports only sync commands without CancellationToken.");
                        }

                        callArgs[i] = cancellationTokenExpression;
                        break;

                    case ParameterRole.InjectedService:
                        callArgs[i] = Expression.Constant(slot.InjectedValue, slot.ParameterType);
                        break;

                    default:
                        if (plan.IsRawArray)
                        {
                            callArgs[i] = arrayEx;
                        }
                        else if (plan.IsRawMemory)
                        {
                            callArgs[i] = memoryEx;
                        }
                        else
                        {
                            callArgs[i] = BuildArgumentConversionExpression(arrayEx, slot.ArgumentIndex, slot.ParameterType, asMethod);
                        }

                        break;
                }
            }

            return Expression.Call(instanceEx, methodInfo, callArgs);
        }

        /// <summary>
        /// args配列上の1要素を、期待する型へ変換するExpressionを構築する.
        /// </summary>
        private static Expression BuildArgumentConversionExpression(ParameterExpression arrayEx, int argumentIndex, Type parameterType, MethodInfo asMethod)
        {
            var indexEx = Expression.ArrayIndex(arrayEx, Expression.Constant(argumentIndex));
            var asGenericMethod = asMethod!.MakeGenericMethod(parameterType);
            var asGenericEx = Expression.Call(indexEx, asGenericMethod);

            // 実行時に引数型の変換を検知したら例外をthrowさせる.
            var catchBlock = Expression.Catch(
                typeof(FormatException),
                Expression.Throw(
                    Expression.New(
                        typeof(CommandFormatException).GetConstructor(new[] { typeof(int), typeof(string), typeof(Type), typeof(Exception) })!,
                        Expression.Constant(argumentIndex),
                        Expression.Property(indexEx, nameof(CommandArgument.String)),
                        Expression.Constant(parameterType, typeof(Type)),
                        Expression.Constant(null, typeof(Exception))
                    ), parameterType
                )
            );

            return Expression.TryCatch(asGenericEx, catchBlock);
        }

        /// <summary>
        /// <see cref="ReadOnlyMemory{CommandArgument}"/> を配列変数へ変換代入するExpressionを構築する.
        /// </summary>
        private static Expression BuildArgsToArrayAssign(ParameterExpression memoryEx, ParameterExpression arrayEx)
        {
            var toArrayMethod = typeof(ReadOnlyMemory<CommandArgument>).GetMethod(nameof(ReadOnlyMemory<CommandArgument>.ToArray))!;
            return Expression.Assign(arrayEx, Expression.Call(memoryEx, toArrayMethod));
        }

        /// <summary>
        /// 引数個数不正時にthrowするExpressionを構築する.
        /// </summary>
        private static Expression BuildArgumentCountThrow(ParameterExpression arrayEx, int minArgCount, int maxArgCount, Type resultType)
        {
            return Expression.Throw(
                Expression.New(
                    typeof(CommandArgumentException).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(Exception) })!,
                    Expression.Property(arrayEx, "Length"),
                    Expression.Constant(minArgCount),
                    Expression.Constant(maxArgCount),
                    Expression.Constant(null, typeof(Exception))
                ), resultType);
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
        /// デフォルト引数数の算出.
        /// </summary>
        private static int GetCommandArgumentCount(MethodInfo methodInfo, in ModeServiceBundle services)
        {
            return ParameterPlan.Build(methodInfo, services).CommandArgumentCount;
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

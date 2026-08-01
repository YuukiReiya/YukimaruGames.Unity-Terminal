using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Abstractions.Attributes;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
using YukimaruGames.Terminal.Infrastructure.Factories;

namespace YukimaruGames.Terminal.Tests.EditMode.Infrastructure.Factories
{
    /// <summary>
    /// <see cref="CommandFactory"/> によるコマンドハンドラー生成を検証するテストクラス。
    /// </summary>
    [TestFixture]
    public sealed class CommandFactoryTests
    {
        private static readonly CommandArgument[] Arguments =
        {
            new("42"),
            new("true"),
        };

        [SetUp]
        public void SetUp()
        {
            SyncCommands.Reset();
            AsyncCommands.Reset();
        }

        /// <summary>
        /// 引数なしの同期voidメソッドからハンドラーが生成され、呼び出しでメソッドが実行されることを検証します。
        /// </summary>
        [Test]
        public void Create_MethodInfo_SyncVoidMethod_BuildsHandler()
        {
            var method = typeof(SyncCommands).GetMethod(nameof(SyncCommands.NoArgs))!;

            var handler = CommandFactory.Create(method);

            Assert.That(handler.IsAsync, Is.False);
            Assert.That(handler.Meta.Command, Is.EqualTo("sync.noargs"));
            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);
            Assert.That(SyncCommands.NoArgsCalled, Is.True);
        }

        /// <summary>
        /// <see cref="ReadOnlyMemory{T}"/> を受け取るメソッドに引数がそのまま渡されることを検証します。
        /// </summary>
        [Test]
        public void Create_MethodInfo_ReadOnlyMemoryArgumentMethod_PassesArguments()
        {
            var method = typeof(SyncCommands).GetMethod(nameof(SyncCommands.MemoryArgs))!;

            var handler = CommandFactory.Create(method);
            handler.Proc(Arguments.AsMemory());

            Assert.That(SyncCommands.MemoryArgsCalled, Is.True);
            Assert.That(SyncCommands.ReceivedMemory.Length, Is.EqualTo(Arguments.Length));
            Assert.That(SyncCommands.ReceivedMemory.Span.SequenceEqual(Arguments), Is.True);
        }

        /// <summary>
        /// <see cref="CommandArgument"/> 配列を受け取るメソッドに引数が変換されて渡されることを検証します。
        /// </summary>
        [Test]
        public void Create_MethodInfo_ArrayArgumentMethod_PassesArguments()
        {
            var method = typeof(SyncCommands).GetMethod(nameof(SyncCommands.ArrayArgs))!;

            var handler = CommandFactory.Create(method);
            handler.Proc(Arguments.AsMemory());

            Assert.That(SyncCommands.ArrayArgsCalled, Is.True);
            Assert.That(SyncCommands.ReceivedArray, Is.Not.Null);
            Assert.That(SyncCommands.ReceivedArray!.Length, Is.EqualTo(Arguments.Length));
            Assert.That(SyncCommands.ReceivedArray.SequenceEqual(Arguments), Is.True);
        }

        /// <summary>
        /// 個別引数型のメソッドに対し、文字列引数が期待の型へ変換されて呼び出されることを検証します。
        /// </summary>
        [Test]
        public void Create_MethodInfo_ConvertibleArguments_ConvertsAndInvokesMethod()
        {
            var method = typeof(SyncCommands).GetMethod(nameof(SyncCommands.ConvertArgs))!;

            var handler = CommandFactory.Create(method);
            handler.Proc(Arguments.AsMemory());

            Assert.That(SyncCommands.ConvertArgsCalled, Is.True);
            Assert.That(SyncCommands.ReceivedInt, Is.EqualTo(42));
            Assert.That(SyncCommands.ReceivedBool, Is.True);
        }

        /// <summary>
        /// インスタンスメソッドからハンドラーが生成され、対象インスタンスに対して呼び出されることを検証します。
        /// </summary>
        [Test]
        public void Create_InstanceMethod_BuildsHandler()
        {
            var instance = new SyncCommands();
            var method = typeof(SyncCommands).GetMethod(nameof(SyncCommands.InstanceArgs))!;

            var handler = CommandFactory.Create(instance, "sync.instance", method);
            handler.Proc(Arguments.AsMemory());

            Assert.That(SyncCommands.InstanceArgsCalled, Is.True);
            Assert.That(SyncCommands.ReceivedMemory.Length, Is.EqualTo(Arguments.Length));
        }

        /// <summary>
        /// 同期デリゲートからハンドラーが生成され、呼び出しで委譲先が実行されることを検証します。
        /// </summary>
        [Test]
        public void Create_Delegate_SyncMemoryDelegate_BuildsHandler()
        {
            ReadOnlyMemory<CommandArgument> received = default;
            Action<ReadOnlyMemory<CommandArgument>> proc = args => received = args;

            var handler = CommandFactory.Create(proc);
            handler.Proc(Arguments.AsMemory());

            Assert.That(received.Length, Is.EqualTo(Arguments.Length));
            Assert.That(received.Span.SequenceEqual(Arguments), Is.True);
        }

        /// <summary>
        /// <see cref="ValueTask"/> を返す非同期メソッドから非同期ハンドラーが生成されることを検証します。
        /// </summary>
        [Test]
        public async Task Create_MethodInfo_ValueTaskAsyncMethod_BuildsAsyncHandler()
        {
            var method = typeof(AsyncCommands).GetMethod(nameof(AsyncCommands.MemoryValueTaskAsync))!;

            var handler = CommandFactory.Create(method);
            await handler.AsyncProc(Arguments.AsMemory(), CancellationToken.None);

            Assert.That(handler.IsAsync, Is.True);
            Assert.That(AsyncCommands.MemoryValueTaskAsyncCalled, Is.True);
            Assert.That(AsyncCommands.ReceivedMemory.Length, Is.EqualTo(Arguments.Length));
            Assert.That(AsyncCommands.ReceivedMemory.Span.SequenceEqual(Arguments), Is.True);
        }

        /// <summary>
        /// 配列引数を受け取る非同期メソッドに引数が正しく渡されることを検証します。
        /// </summary>
        [Test]
        public async Task Create_MethodInfo_ArrayValueTaskAsyncMethod_PassesArguments()
        {
            var method = typeof(AsyncCommands).GetMethod(nameof(AsyncCommands.ArrayValueTaskAsync))!;

            var handler = CommandFactory.Create(method);
            await handler.AsyncProc(Arguments.AsMemory(), CancellationToken.None);

            Assert.That(handler.IsAsync, Is.True);
            Assert.That(AsyncCommands.ArrayValueTaskAsyncCalled, Is.True);
            Assert.That(AsyncCommands.ReceivedArray, Is.Not.Null);
            Assert.That(AsyncCommands.ReceivedArray!.Length, Is.EqualTo(Arguments.Length));
            Assert.That(AsyncCommands.ReceivedArray.SequenceEqual(Arguments), Is.True);
        }

        /// <summary>
        /// <see cref="Task"/> を返す非同期メソッドに <see cref="CancellationToken"/> と引数が正しく渡されることを検証します。
        /// </summary>
        [Test]
        public async Task Create_MethodInfo_TaskAsyncMethod_PassesCancellationToken()
        {
            var method = typeof(AsyncCommands).GetMethod(nameof(AsyncCommands.TaskAsync))!;
            using var cts = new CancellationTokenSource();
            var arguments = new[] { new CommandArgument("42") };

            var handler = CommandFactory.Create(method);
            await handler.AsyncProc(arguments.AsMemory(), cts.Token);

            Assert.That(handler.IsAsync, Is.True);
            Assert.That(AsyncCommands.TaskAsyncCalled, Is.True);
            Assert.That(AsyncCommands.ReceivedCancellationToken, Is.EqualTo(cts.Token));
            Assert.That(AsyncCommands.ReceivedInt, Is.EqualTo(42));
        }

        /// <summary>
        /// 非同期デリゲートからハンドラーが生成され、呼び出しで委譲先が実行されることを検証します。
        /// </summary>
        [Test]
        public async Task Create_Delegate_AsyncDelegate_BuildsHandler()
        {
            var received = default(ReadOnlyMemory<CommandArgument>);
            Func<ReadOnlyMemory<CommandArgument>, CancellationToken, ValueTask> proc = async (args, _) =>
            {
                received = args;
                await Task.CompletedTask;
            };

            var handler = CommandFactory.Create(proc);
            await handler.AsyncProc(Arguments.AsMemory(), CancellationToken.None);

            Assert.That(handler.IsAsync, Is.True);
            Assert.That(received.Length, Is.EqualTo(Arguments.Length));
            Assert.That(received.Span.SequenceEqual(Arguments), Is.True);
        }

        /// <summary>
        /// <c>async void</c> メソッドを指定した場合に <see cref="NotSupportedException"/> が送出されることを検証します。
        /// </summary>
        [Test]
        public void Create_AsyncVoidMethod_ThrowsNotSupportedException()
        {
            var method = typeof(SyncCommands).GetMethod(nameof(SyncCommands.AsyncVoidArgs))!;

            var ex = Assert.Throws<NotSupportedException>(() => CommandFactory.Create(method));
            Assert.That(ex!.Message, Does.Contain("async"));
        }

        private sealed class SyncCommands
        {
            public static bool NoArgsCalled { get; private set; }
            public static bool MemoryArgsCalled { get; private set; }
            public static bool ArrayArgsCalled { get; private set; }
            public static bool ConvertArgsCalled { get; private set; }
            public static bool InstanceArgsCalled { get; private set; }
            public static ReadOnlyMemory<CommandArgument> ReceivedMemory { get; private set; }
            public static CommandArgument[] ReceivedArray { get; private set; }
            public static int ReceivedInt { get; private set; }
            public static bool ReceivedBool { get; private set; }

            public static void Reset()
            {
                NoArgsCalled = false;
                MemoryArgsCalled = false;
                ArrayArgsCalled = false;
                ConvertArgsCalled = false;
                InstanceArgsCalled = false;
                ReceivedMemory = default;
                ReceivedArray = null;
                ReceivedInt = default;
                ReceivedBool = default;
            }

            [TerminalCommand("sync.noargs", help: "no args")]
            public static void NoArgs()
            {
                NoArgsCalled = true;
            }

            [TerminalCommand("sync.memory", maxArgCount: 1, minArgCount: 1, help: "memory")]
            public static void MemoryArgs(ReadOnlyMemory<CommandArgument> args)
            {
                MemoryArgsCalled = true;
                ReceivedMemory = args;
            }

            [TerminalCommand("sync.array", maxArgCount: 1, minArgCount: 1, help: "array")]
            public static void ArrayArgs(CommandArgument[] args)
            {
                ArrayArgsCalled = true;
                ReceivedArray = args;
            }

            [TerminalCommand("sync.convert", maxArgCount: 2, minArgCount: 2, help: "convert")]
            public static void ConvertArgs(int value, bool flag)
            {
                ConvertArgsCalled = true;
                ReceivedInt = value;
                ReceivedBool = flag;
            }

            [TerminalCommand("sync.instance", maxArgCount: 1, minArgCount: 1, help: "instance")]
            public void InstanceArgs(ReadOnlyMemory<CommandArgument> args)
            {
                InstanceArgsCalled = true;
                ReceivedMemory = args;
            }

            [TerminalCommand("async.void", help: "async void")]
            public static async void AsyncVoidArgs()
            {
                await Task.CompletedTask;
            }
        }

        private static class AsyncCommands
        {
            public static bool MemoryValueTaskAsyncCalled { get; private set; }
            public static bool ArrayValueTaskAsyncCalled { get; private set; }
            public static bool TaskAsyncCalled { get; private set; }
            public static ReadOnlyMemory<CommandArgument> ReceivedMemory { get; private set; }
            public static CommandArgument[] ReceivedArray { get; private set; }
            public static CancellationToken ReceivedCancellationToken { get; private set; }
            public static int ReceivedInt { get; private set; }

            public static void Reset()
            {
                MemoryValueTaskAsyncCalled = false;
                ArrayValueTaskAsyncCalled = false;
                TaskAsyncCalled = false;
                ReceivedMemory = default;
                ReceivedArray = null;
                ReceivedCancellationToken = default;
                ReceivedInt = default;
            }

            [TerminalCommand("async.memory", maxArgCount: 1, minArgCount: 1, help: "memory")]
            public static async ValueTask MemoryValueTaskAsync(ReadOnlyMemory<CommandArgument> args)
            {
                MemoryValueTaskAsyncCalled = true;
                ReceivedMemory = args;
                await Task.CompletedTask;
            }

            [TerminalCommand("async.array", maxArgCount: 1, minArgCount: 1, help: "array")]
            public static async ValueTask ArrayValueTaskAsync(CommandArgument[] args)
            {
                ArrayValueTaskAsyncCalled = true;
                ReceivedArray = args;
                await Task.CompletedTask;
            }

            [TerminalCommand("async.task", maxArgCount: 1, minArgCount: 1, help: "task")]
            public static async Task TaskAsync(int value, CancellationToken cancellationToken)
            {
                TaskAsyncCalled = true;
                ReceivedInt = value;
                ReceivedCancellationToken = cancellationToken;
                await Task.CompletedTask;
            }
        }
    }
}

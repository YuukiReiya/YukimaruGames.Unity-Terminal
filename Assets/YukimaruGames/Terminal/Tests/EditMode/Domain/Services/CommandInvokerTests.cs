using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Services;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Services
{
    [TestFixture]
    public sealed class CommandInvokerTests
    {
        private CommandInvoker _sut;

        // ─── テスト用ハンドラファクトリ ───────────────────────────────────────

        private static CommandHandler MakeSyncHandler(Action<ReadOnlyMemory<CommandArgument>> proc)
            => new CommandHandler((CommandDelegate)(args => proc(args)), "cmd", 0, 0, "");

        private static CommandHandler MakeAsyncHandler(
            Func<ReadOnlyMemory<CommandArgument>, CancellationToken, ValueTask> proc)
            => new CommandHandler((CommandAsyncDelegate)((args, ct) => proc(args, ct)), "cmd", 0, 0, "");

        [SetUp]
        public void SetUp() => _sut = new CommandInvoker();

        // ─── Execute（同期）────────────────────────────────────────────────────

        [Test]
        public void Execute_SyncHandler_InvokesProc()
        {
            var called = false;
            var handler = MakeSyncHandler(_ => called = true);

            _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty);

            Assert.IsTrue(called);
        }

        [Test]
        public void Execute_SyncHandler_PassesArgumentsToProc()
        {
            ReadOnlyMemory<CommandArgument> received = default;
            var handler = MakeSyncHandler(args => received = args);
            var arguments = new CommandArgument[1];

            _sut.Execute(handler, arguments.AsMemory());

            Assert.AreEqual(1, received.Length);
        }

        [Test]
        public void Execute_SyncHandler_EmptyArguments_DoesNotThrow()
        {
            var handler = MakeSyncHandler(_ => { });

            Assert.DoesNotThrow(() =>
                _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty));
        }

        [Test]
        public void Execute_ProcThrows_ExceptionPropagates()
        {
            var handler = MakeSyncHandler(_ => throw new InvalidOperationException("test"));

            Assert.Throws<InvalidOperationException>(() =>
                _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty));
        }

        [Test]
        public void Execute_HandlerWithNullProc_DoesNotThrow()
        {
            // Proc が null の場合（AsyncHandler）は Invoke されない
            var handler = MakeAsyncHandler((_, __) => default);

            Assert.DoesNotThrow(() =>
                _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty));
        }

        // ─── ExecuteAsync（非同期）────────────────────────────────────────────

        [Test]
        public async Task ExecuteAsync_AsyncHandler_InvokesAsyncProc()
        {
            var called = false;
            var handler = MakeAsyncHandler(async (_, __) => { called = true; await Task.CompletedTask; });

            await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None);

            Assert.IsTrue(called);
        }

        [Test]
        public async Task ExecuteAsync_AsyncHandler_PassesArgumentsToProc()
        {
            ReadOnlyMemory<CommandArgument> received = default;
            var handler = MakeAsyncHandler((args, _) => { received = args; return default; });
            var arguments = new CommandArgument[2];

            await _sut.ExecuteAsync(handler, arguments.AsMemory(), CancellationToken.None);

            Assert.AreEqual(2, received.Length);
        }

        [Test]
        public async Task ExecuteAsync_AsyncHandler_PassesCancellationToken()
        {
            CancellationToken received = default;
            var handler = MakeAsyncHandler((_, ct) => { received = ct; return default; });
            using var cts = new CancellationTokenSource();

            await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, cts.Token);

            Assert.AreEqual(cts.Token, received);
        }

        [Test]
        public void ExecuteAsync_AsyncProcThrows_ExceptionPropagates()
        {
            var handler = MakeAsyncHandler((_, __) => throw new InvalidOperationException("async error"));

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None));
        }

        [Test]
        public void ExecuteAsync_CancelledToken_ThrowsOperationCanceledException()
        {
            var handler = MakeAsyncHandler(async (_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.CompletedTask;
            });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.CatchAsync<OperationCanceledException>(async () =>
                await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, cts.Token));
        }

        [Test]
        public async Task ExecuteAsync_ActiveToken_CompletesNormally()
        {
            var handler = MakeAsyncHandler((_, ct) => default);

            Assert.DoesNotThrowAsync(async () =>
                await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None));
        }
    }
}
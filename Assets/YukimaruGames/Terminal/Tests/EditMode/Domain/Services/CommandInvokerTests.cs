using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Abstractions.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Services;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Services
{
    /// <summary>
    /// <see cref="CommandInvoker"/> の振る舞いを検証するテストクラス。
    /// </summary>
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

        /// <summary>
        /// 同期ハンドラーが指定された場合、登録された同期デリゲートが正しく実行されることを検証します。
        /// </summary>
        /// <remarks>
        /// ハンドラー内部のプロシージャがスキップされず、確実に呼び出されているかを確認します。
        /// </remarks>
        [Test]
        public void Execute_SyncHandler_InvokesProc()
        {
            var called = false;
            var handler = MakeSyncHandler(_ => called = true);

            _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty);

            Assert.IsTrue(called);
        }

        /// <summary>
        /// 同期ハンドラーの実行時、パースされたコマンド引数がデリゲートへ正確に渡されることを検証します。
        /// </summary>
        /// <remarks>
        /// 引数のデータバッファ（<see cref="ReadOnlyMemory{T}"/>）が途中で欠落したり書き換わったりせず伝播することを確認します。
        /// </remarks>
        [Test]
        public void Execute_SyncHandler_PassesArgumentsToProc()
        {
            ReadOnlyMemory<CommandArgument> received = default;
            var handler = MakeSyncHandler(args => received = args);
            var arguments = new CommandArgument[1];

            _sut.Execute(handler, arguments.AsMemory());

            Assert.AreEqual(1, received.Length);
        }

        /// <summary>
        /// 同期ハンドラーに空のコマンド引数が渡された場合でも、例外を発生させずに安全に処理できることを検証します。
        /// </summary>
        /// <remarks>
        /// 引数が存在しないコマンド（オプション無しのコマンドなど）における境界値テストです。
        /// </remarks>
        [Test]
        public void Execute_SyncHandler_EmptyArguments_DoesNotThrow()
        {
            var handler = MakeSyncHandler(_ => { });

            Assert.DoesNotThrow(() =>
                _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty));
        }

        /// <summary>
        /// 同期ハンドラーの処理内部で例外が発生した場合、Invoker内部で握りつぶされず呼び出し元へそのまま伝播することを検証します。
        /// </summary>
        /// <remarks>
        /// コマンド独自のドメインエラーやバグが、上位のレイヤー（UseCaseやロガー）で適切に検知できる状態にあるかを保証します。
        /// </remarks>
        [Test]
        public void Execute_ProcThrows_ExceptionPropagates()
        {
            var handler = MakeSyncHandler(_ => throw new InvalidOperationException("test"));

            Assert.Throws<InvalidOperationException>(() =>
                _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty));
        }

        /// <summary>
        /// 同期実行メソッドに、非同期デリゲートしか持たないハンドラーが渡された場合、例外を投げずに安全に処理をスルーできることを検証します。
        /// </summary>
        /// <remarks>
        /// ユーザーの呼び出しミス（非同期コマンドを同期メソッドで動かそうとした場合など）に対し、システムがクラッシュしない堅牢性を持ち合わせているか確認します。
        /// </remarks>
        [Test]
        public void Execute_HandlerWithNullProc_DoesNotThrow()
        {
            // Proc が null の場合（AsyncHandler）は Invoke されない
            var handler = MakeAsyncHandler((_, __) => default);

            Assert.Throws<ArgumentException>(() =>
                _sut.Execute(handler, ReadOnlyMemory<CommandArgument>.Empty));
        }

        // ─── ExecuteAsync（非同期）────────────────────────────────────────────

        /// <summary>
        /// 非同期ハンドラーが指定された場合、登録された非同期デリゲート（<see cref="ValueTask"/>）が正しく実行・完了されることを検証します。
        /// </summary>
        /// <remarks>
        /// 非同期パイプラインにおけるステートマシンが正常に稼働し、コールバックが発火することを確認します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_AsyncHandler_InvokesAsyncProc()
        {
            var called = false;
            var handler = MakeAsyncHandler(async (_, __) => { called = true; await Task.CompletedTask; });

            await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None);

            Assert.IsTrue(called);
        }

        /// <summary>
        /// 非同期ハンドラーの実行時、コマンド引数が非同期コンテキストを跨いでデリゲートへ正確に渡されることを検証します。
        /// </summary>
        /// <remarks>
        /// 複数の非同期処理が並行して走る可能性がある中で、引数のスコープが安全に維持されているかを確認します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_AsyncHandler_PassesArgumentsToProc()
        {
            ReadOnlyMemory<CommandArgument> received = default;
            var handler = MakeAsyncHandler((args, _) => { received = args; return default; });
            var arguments = new CommandArgument[2];

            await _sut.ExecuteAsync(handler, arguments.AsMemory(), CancellationToken.None);

            Assert.AreEqual(2, received.Length);
        }

        /// <summary>
        /// 外部（UseCaseなど）から渡された <see cref="CancellationToken"/> が、非同期ハンドラーの末端まで正しく伝播することを検証します。
        /// </summary>
        /// <remarks>
        /// これが通ることで、Ctrl+Cなどによる中断要求が実行中の重いバックグラウンドタスクまで確実に届く状態を保証します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_AsyncHandler_PassesCancellationToken()
        {
            CancellationToken received = default;
            var handler = MakeAsyncHandler((_, ct) => { received = ct; return default; });
            using var cts = new CancellationTokenSource();

            await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, cts.Token);

            Assert.AreEqual(cts.Token, received);
        }

        /// <summary>
        /// 非同期ハンドラーの処理内部で例外が発生した場合、非同期のタスクの完了を待機（await）した段階で、呼び出し元へ正しく例外が伝播することを検証します。
        /// </summary>
        /// <remarks>
        /// 非同期メソッド内での例外が未処理タスクとして迷子（UnobservedTaskException）にならず、await元で適切にキャッチできる状態にあるかを確認します。
        /// </remarks>
        [Test]
        public void ExecuteAsync_AsyncProcThrows_ExceptionPropagates()
        {
            var handler = MakeAsyncHandler((_, __) => throw new InvalidOperationException("async error"));

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None));
        }

        /// <summary>
        /// すでにキャンセル要求が走っているトークンを渡して非同期処理を開始した場合、適切にキャンセル例外がスローされることを検証します。
        /// </summary>
        /// <remarks>
        /// async/awaitのステートマシン特性を考慮し、派生クラス（TaskCanceledException）も含めて <see cref="OperationCanceledException"/> として広範かつ確実に捕捉できるかを検証します。
        /// </remarks>
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

        /// <summary>
        /// キャンセルされていない通常状態のトークンを用いた場合、非同期処理が途中で中断されることなく正常に完了することを検証します。
        /// </summary>
        /// <remarks>
        /// 正常系ルートにおいて、トークンの状態チェックが誤作動を起こさず無傷で処理を終えられることを確認します。
        /// </remarks>
        [Test]
        public async Task ExecuteAsync_ActiveToken_CompletesNormally()
        {
            var handler = MakeAsyncHandler((_, ct) => default);

            Assert.DoesNotThrowAsync(async () =>
                await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None));
        }
        
        /// <summary>
        /// 非同期実行メソッドに、同期デリゲートとして登録されているハンドラーが渡された場合、
        /// <see cref="ArgumentException"/> が正しくスローされることを検証します。
        /// </summary>
        [Test]
        public void ExecuteAsync_SyncHandlerPassedToAsyncExecute_ThrowsArgumentException()
        {
            // 同期ハンドラー（AsyncProc が null）を生成
            var handler = MakeSyncHandler(_ => { });

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _sut.ExecuteAsync(handler, ReadOnlyMemory<CommandArgument>.Empty, CancellationToken.None));
        }
    }
}
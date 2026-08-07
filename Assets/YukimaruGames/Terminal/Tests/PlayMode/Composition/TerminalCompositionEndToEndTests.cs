using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using YukimaruGames.Terminal.Composition;
using YukimaruGames.Terminal.Infrastructure.Factories;
using YukimaruGames.Terminal.Presentation.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Window;
using YukimaruGames.Terminal.Presentation.Presenters;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.PlayMode.Composition
{
    /// <summary>
    /// <see cref="TerminalStandardInstaller"/>による実配線(DI)から、
    /// コマンド登録・実行・ログ出力までの一連のユーザー操作フローを検証する.
    /// </summary>
    /// <remarks>
    /// <see cref="TerminalBootstrapper"/>のAwake/OnDestroyと同じ手順(Install/Uninstall)を踏むことで、
    /// Compositionレイヤーのリネーム・再配線後も実際の起動シーケンスが壊れていないことを確認する.
    /// </remarks>
    [TestFixture]
    public sealed class TerminalCompositionEndToEndTests
    {
        private IInstaller _installer;
        private TerminalRuntimeScope _scope;

        /// <summary>
        /// 各テスト実行前に<see cref="TerminalStandardInstaller"/>でDI配線を構築し、
        /// <see cref="TerminalRuntimeScope"/>を取得する(<see cref="TerminalBootstrapper"/>のAwake相当).
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _installer = new TerminalStandardInstaller();
            _scope = _installer.Install();
        }

        /// <summary>
        /// 各テスト実行後に<see cref="TerminalRuntimeScope"/>を破棄する(<see cref="TerminalBootstrapper"/>のOnDestroy相当).
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (_scope != null)
            {
                _installer.Uninstall(_scope);
                _scope = null;
            }
        }

        /// <summary>Installにより主要コンポーネントが一通り解決されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Install_ResolvesAllPrimaryComponents()
        {
            yield return null;

            Assert.IsNotNull(_scope.EntryPoint);
            Assert.IsNotNull(_scope.Service);
            Assert.IsNotNull(_scope.Registry);
            Assert.IsNotNull(_scope.Autocomplete);
            Assert.IsNotNull(_scope.View);
        }

        /// <summary>コマンド登録後にExecuteAsyncで実行すると、入力(Entry)ログと実行結果ログが記録されることを検証する.</summary>
        [UnityTest]
        public IEnumerator ExecuteAsync_RegisteredCommand_LogsEntryAndCommandOutput()
        {
            yield return null;

            // NOTE: "echo" は組み込みコマンド(TerminalGeneralCommands)が既に使用しているため、
            // このテスト専用のダミーコマンド名として "test.echo" を使う.
            System.Action echo = () => _scope.Service.Message("hello");
            Assert.IsTrue(_scope.Registry.Add("test.echo", CommandFactory.Create(echo)));

            var task = _scope.Service.ExecuteAsync("test.echo", CancellationToken.None).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            var logs = _scope.Service.Logs;
            Assert.IsTrue(logs.Any(l => l.MessageType == MessageType.Entry && l.Message == "test.echo"));
            Assert.IsTrue(logs.Any(l => l.MessageType == MessageType.Message && l.Message == "hello"));
        }

        /// <summary>未登録コマンドを実行すると、実行はされずエラーログのみが記録されることを検証する.</summary>
        [UnityTest]
        public IEnumerator ExecuteAsync_UnknownCommand_LogsErrorOnly()
        {
            yield return null;

            var task = _scope.Service.ExecuteAsync("no-such-command", CancellationToken.None).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            var logs = _scope.Service.Logs;
            Assert.IsTrue(logs.Any(
                l => l.MessageType == MessageType.Error && l.Message == "No such command: 'no-such-command'."));
        }

        /// <summary>コマンド登録を解除すると、以後の実行がエラーログのみになり、ハンドラーが呼ばれないことを検証する.</summary>
        [UnityTest]
        public IEnumerator UnregisterCommand_PreventsFurtherExecution()
        {
            yield return null;

            var callCount = 0;
            System.Action increment = () => callCount++;
            _scope.Registry.Add("inc", CommandFactory.Create(increment));
            _scope.Autocomplete.Register("inc");

            var firstTask = _scope.Service.ExecuteAsync("inc", CancellationToken.None).AsTask();
            yield return new WaitUntil(() => firstTask.IsCompleted);
            Assert.AreEqual(1, callCount);

            Assert.IsTrue(_scope.Registry.Remove("inc"));
            _scope.Autocomplete.Unregister("inc");

            var secondTask = _scope.Service.ExecuteAsync("inc", CancellationToken.None).AsTask();
            yield return new WaitUntil(() => secondTask.IsCompleted);

            Assert.AreEqual(1, callCount, "登録解除後はハンドラーが呼ばれてはならない");
            Assert.IsTrue(_scope.Service.Logs.Any(
                l => l.MessageType == MessageType.Error && l.Message == "No such command: 'inc'."));
        }

        /// <summary>登録済みコマンド名が自動補完候補に反映されることを検証する.</summary>
        [UnityTest]
        public IEnumerator RegisterCommand_ReflectsInAutocomplete()
        {
            yield return null;

            System.Action noop = () => { };
            _scope.Registry.Add("greet", CommandFactory.Create(noop));
            _scope.Autocomplete.Register("greet");

            var candidates = _scope.Service.Autocomplete("gre");

            Assert.Contains("greet", candidates);
        }

        /// <summary>ResetLogsを呼ぶとログが全て消去されることを検証する.</summary>
        [UnityTest]
        public IEnumerator ResetLogs_ClearsAllLogs()
        {
            yield return null;

            _scope.Service.Message("before-reset");
            Assert.IsNotEmpty(_scope.Service.Logs);

            _scope.Service.ResetLogs();

            Assert.IsEmpty(_scope.Service.Logs);
        }

        /// <summary>
        /// EntryPoint.Update()を複数フレーム駆動しても例外が発生しないことを検証する.
        /// </summary>
        /// <remarks>
        /// TerminalAction.Cancelがキーマップ(InputSystemKey/LegacyInputKey)に未実装だったため、
        /// EventListener.Updateが全アクションを巡回する毎フレームでArgumentOutOfRangeExceptionが
        /// 発生し続けていたリグレッション(実機Play mode検証で発覚)の再発防止用.
        /// </remarks>
        [UnityTest]
        public IEnumerator EntryPointUpdate_MultipleFrames_DoesNotThrow()
        {
            yield return null;

            for (var i = 0; i < 10; ++i)
            {
                Assert.DoesNotThrow(() => _scope.EntryPoint.Update());
                yield return null;
            }
        }

        /// <summary>
        /// IME変換中(IsImeComposing=true)のままでも、Closeアクションでウィンドウが閉じられることを検証する.
        /// </summary>
        /// <remarks>
        /// compositionStringが何らかの理由でクリアされないまま残るケース(実機のCGEventPost経由の
        /// 合成キー入力で確認)で、IsImeComposingガードによりウィンドウが永久に閉じられなくなる
        /// リグレッションの再発防止用。Open/Executeは誤発火の実害が大きいためガードを維持するが、
        /// Closeは常に許可する設計としている.
        /// </remarks>
        [UnityTest]
        public IEnumerator OnCloseTriggered_WhileImeComposing_StillClosesWindow()
        {
            yield return null;

            var disposablesField = typeof(TerminalRuntimeScope).GetField("_disposables", BindingFlags.NonPublic | BindingFlags.Instance);
            var disposables = (System.Collections.IEnumerable)disposablesField!.GetValue(_scope);
            TerminalCoordinator coordinator = null;
            InputPresenter inputPresenter = null;
            foreach (var d in disposables)
            {
                if (d is TerminalCoordinator c) coordinator = c;
                if (d is InputPresenter ip) inputPresenter = ip;
            }

            Assert.IsNotNull(coordinator);
            Assert.IsNotNull(inputPresenter);

            var windowPresenterField = typeof(TerminalCoordinator).GetField("_windowPresenter", BindingFlags.NonPublic | BindingFlags.Instance);
            var windowPresenter = (IWindowPresenter)windowPresenterField!.GetValue(coordinator);
            windowPresenter.Open();

            // Openアニメーションが完了するまで待つ(完了しないとClose()がIsAnimatingガードで無視される).
            // バッチモードではフレームごとのdeltaTimeが極small/不安定なため、フレーム数ではなく
            // 実時間で上限を設ける(アニメーションが完了しない不具合発生時に無限ハングしないための保険).
            var deadline = Time.realtimeSinceStartup + 10f;
            while (windowPresenter.IsAnimating)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Openアニメーションが規定時間内に完了しませんでした");
                _scope.EntryPoint.Update();
                yield return null;
            }

            var isImeComposingProp = typeof(InputPresenter).GetProperty(nameof(IInputPresenter.IsImeComposing));
            isImeComposingProp!.SetValue(inputPresenter, true);

            var onCloseMethod = typeof(TerminalCoordinator).GetMethod("OnCloseTriggered", BindingFlags.NonPublic | BindingFlags.Instance);
            onCloseMethod!.Invoke(coordinator, null);

            var windowAnimProviderField = typeof(TerminalCoordinator).GetField("_windowAnimationProvider", BindingFlags.NonPublic | BindingFlags.Instance);
            var windowAnimProvider = windowAnimProviderField!.GetValue(coordinator);
            var state = windowAnimProvider.GetType().GetProperty("State")!.GetValue(windowAnimProvider);

            Assert.AreEqual(WindowState.Close, state);
        }

        /// <summary>
        /// terminal.stack診断コマンド(static + IModeStackInspector/IModeOutput注入)が
        /// 実際の配線(TerminalStandardInstaller)経由で動作することを検証する.
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteAsync_TerminalStackCommand_LogsNormalMode()
        {
            yield return null;

            var task = _scope.Service.ExecuteAsync("terminal.stack", CancellationToken.None).AsTask();
            yield return new WaitUntil(() => task.IsCompleted);

            var logs = _scope.Service.Logs;
            var dump = string.Join(" | ", logs.Select(l => $"[{l.MessageType}] {l.Message}"));
            Assert.IsTrue(logs.Any(l => l.MessageType == MessageType.Message && l.Message.Contains("normal")), $"logs: {dump}");
        }
    }
}

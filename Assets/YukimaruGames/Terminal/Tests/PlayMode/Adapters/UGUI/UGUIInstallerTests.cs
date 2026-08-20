using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.UGUI;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.UGUI
{
    /// <summary>
    /// <see cref="UGUIInstaller"/>の構築と破棄を検証する.
    /// </summary>
    /// <remarks>
    /// <see cref="TerminalBootstrapper"/>と同じ手順(Install → EntryPoint.Start → Uninstall)を踏む。
    /// <c>EventSystem</c>の解決は<c>IStartable</c>で行われるため、<c>Install()</c>だけでは走らない(#152).
    /// </remarks>
    [TestFixture]
    public sealed class UGUIInstallerTests
    {
        private IInstaller _installer;
        private TerminalRuntimeScope _scope;
        private readonly List<GameObject> _sceneObjects = new();

        /// <summary>このテストのInstallで生成された<see cref="WindowRoot"/>.</summary>
        /// <remarks>
        /// 種類で検索すると、他のテストや読み込み済みシーンが持つ別のインスタンスを掴みうる。
        /// その場合、生成の検証は<b>別物で成功し</b>、破棄の検証は<b>残っている別物で失敗する</b>。
        /// Installの前後の差分から自分が作ったものを特定する(#173のレビュー指摘).
        /// </remarks>
        private WindowRoot _windowRoot;

        /// <summary>各テスト前に<see cref="UGUIInstaller"/>を用意する.</summary>
        [SetUp]
        public void SetUp()
        {
            _installer = new UGUIInstaller();
            _windowRoot = null;
        }

        /// <summary>各テスト後にScopeを破棄し、テストが生成したGameObjectを片付ける.</summary>
        [TearDown]
        public void TearDown()
        {
            if (_scope != null)
            {
                _installer.Uninstall(_scope);
                _scope = null;
            }

            foreach (var gameObject in _sceneObjects)
            {
                if (gameObject != null) Object.DestroyImmediate(gameObject);
            }

            _sceneObjects.Clear();
        }

        /// <summary>
        /// テストが自分で用意したシーン上の<see cref="EventSystem"/>.
        /// </summary>
        /// <remarks>後始末のため、生成したGameObjectは必ず<see cref="_sceneObjects"/>へ積む.</remarks>
        private EventSystem CreateSceneEventSystem()
        {
            var gameObject = new GameObject("Test EventSystem", typeof(EventSystem));
            _sceneObjects.Add(gameObject);
            return gameObject.GetComponent<EventSystem>();
        }

        private void InstallAndStart()
        {
            var before = new HashSet<WindowRoot>(FindAllWindowRoots());

            _scope = _installer.Install();
            _scope.EntryPoint.Start();

            // 差分は1件であるべき。複数あれば重複生成であり、先頭だけを見ると見逃す.
            var created = FindAllWindowRoots().Where(root => !before.Contains(root)).ToArray();

            Assert.That(created.Length, Is.EqualTo(1), "Installで生成されたWindowRootが1つではない");

            _windowRoot = created[0];
        }

        private static WindowRoot[] FindAllWindowRoots() =>
            Object.FindObjectsByType<WindowRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        /// <summary>Installにより<see cref="Canvas"/>と<see cref="WindowRoot"/>が生成されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Install_CanvasとWindowRootが生成される()
        {
            InstallAndStart();
            yield return null;

            Assert.That(_windowRoot.GetComponent<Canvas>(), Is.Not.Null, "Canvasが生成されていない");
            Assert.That(_windowRoot.IsInitialized, Is.True, "UI要素の解決に失敗している");
        }

        /// <summary>
        /// Prefab未指定でも例外にならず、コード生成の最小構成でUI要素が揃うことを検証する.
        /// </summary>
        /// <remarks>
        /// Prefab未指定は警告付きのフォールバック経路。<c>LogAssert.ignoreFailingMessages</c>は
        /// テスト間で自動的にリセットされず後続のテストへ漏れるため使わず、
        /// 期待する警告を明示的に宣言する.
        /// </remarks>
        [UnityTest]
        public IEnumerator Install_Prefab未指定でもコード生成フォールバックで起動する()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No prefab assigned for the uGUI backend"));

            InstallAndStart();
            yield return null;

            Assert.That(_windowRoot.IsInitialized, Is.True);
            Assert.That(_windowRoot.InputField, Is.Not.Null);
            Assert.That(_windowRoot.SubmitButton, Is.Not.Null);
            Assert.That(_windowRoot.LauncherOpenButton, Is.Not.Null);
            Assert.That(_windowRoot.LauncherCloseButton, Is.Not.Null);
        }

        /// <summary>シーンに<see cref="EventSystem"/>が無い場合、入力モジュール付きで生成されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Start_EventSystemが無ければ生成される()
        {
            Assert.That(EventSystem.current, Is.Null, "前提: シーンにEventSystemが無いこと");

            InstallAndStart();
            yield return null;

            Assert.That(EventSystem.current, Is.Not.Null, "EventSystemが生成されていない");
            Assert.That(EventSystem.current.currentInputModule, Is.Not.Null, "入力モジュールが付いていない");
        }

        /// <summary>
        /// シーンに<see cref="EventSystem"/>が既にある場合、重複生成せず既存を差し替えもしないことを検証する(#152).
        /// </summary>
        [UnityTest]
        public IEnumerator Start_EventSystemが既にあれば生成しない()
        {
            var existing = CreateSceneEventSystem();
            yield return null;

            InstallAndStart();
            yield return null;

            var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Assert.That(all.Length, Is.EqualTo(1), "EventSystemが重複生成されている");
            Assert.That(all[0], Is.SameAs(existing), "既存のEventSystemが差し替えられている");
        }

        /// <summary>Uninstallで自前生成した<see cref="Canvas"/>と<see cref="EventSystem"/>が破棄されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Uninstall_自前生成したCanvasとEventSystemが破棄される()
        {
            InstallAndStart();
            yield return null;

            _installer.Uninstall(_scope);
            _scope = null;
            yield return null;

            // Unityのオブジェクトは破棄後もnull比較でtrueになる(偽装null).
            Assert.That(_windowRoot == null, Is.True, "Installで生成したCanvasが残っている");
            Assert.That(EventSystem.current, Is.Null, "自前生成したEventSystemが残っている");
        }

        /// <summary>Uninstallがシーン側の<see cref="EventSystem"/>を破棄しないことを検証する.</summary>
        [UnityTest]
        public IEnumerator Uninstall_既存のEventSystemは破棄しない()
        {
            var existing = CreateSceneEventSystem();
            yield return null;

            InstallAndStart();
            yield return null;

            _installer.Uninstall(_scope);
            _scope = null;
            yield return null;

            Assert.That(existing, Is.Not.Null, "既存のEventSystemが破棄されている");
            Assert.That(EventSystem.current, Is.SameAs(existing));
        }
    }
}

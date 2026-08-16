using System.Collections;
using System.Collections.Generic;
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

        [SetUp]
        public void SetUp()
        {
            _installer = new UGUIInstaller();
        }

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
            _scope = _installer.Install();
            _scope.EntryPoint.Start();
        }

        [UnityTest]
        public IEnumerator Install_CanvasとWindowRootが生成される()
        {
            InstallAndStart();
            yield return null;

            var windowRoot = Object.FindFirstObjectByType<WindowRoot>();

            Assert.That(windowRoot, Is.Not.Null, "WindowRootが生成されていない");
            Assert.That(windowRoot.GetComponent<Canvas>(), Is.Not.Null, "Canvasが生成されていない");
            Assert.That(windowRoot.IsInitialized, Is.True, "UI要素の解決に失敗している");
        }

        [UnityTest]
        public IEnumerator Install_Prefab未指定でもコード生成フォールバックで起動する()
        {
            // Prefabは未指定(既定)。警告は出るが例外にはならず、要素が揃うこと.
            LogAssert.ignoreFailingMessages = true;

            InstallAndStart();
            yield return null;

            var windowRoot = Object.FindFirstObjectByType<WindowRoot>();

            Assert.That(windowRoot.IsInitialized, Is.True);
            Assert.That(windowRoot.InputField, Is.Not.Null);
            Assert.That(windowRoot.SubmitButton, Is.Not.Null);
            Assert.That(windowRoot.LauncherOpenButton, Is.Not.Null);
            Assert.That(windowRoot.LauncherCloseButton, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Start_EventSystemが無ければ生成される()
        {
            Assert.That(EventSystem.current, Is.Null, "前提: シーンにEventSystemが無いこと");

            InstallAndStart();
            yield return null;

            Assert.That(EventSystem.current, Is.Not.Null, "EventSystemが生成されていない");
            Assert.That(EventSystem.current.currentInputModule, Is.Not.Null, "入力モジュールが付いていない");
        }

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

        [UnityTest]
        public IEnumerator Uninstall_自前生成したCanvasとEventSystemが破棄される()
        {
            InstallAndStart();
            yield return null;

            _installer.Uninstall(_scope);
            _scope = null;
            yield return null;

            Assert.That(Object.FindFirstObjectByType<WindowRoot>(), Is.Null, "Canvasが残っている");
            Assert.That(EventSystem.current, Is.Null, "自前生成したEventSystemが残っている");
        }

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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using YukimaruGames.Terminal.Adapters.UIToolkit;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.UIToolkit
{
    /// <summary>
    /// <see cref="UIToolkitInstaller"/>の構築と破棄を検証する.
    /// </summary>
    /// <remarks>
    /// <see cref="TerminalBootstrapper"/>と同じ手順(Install → EntryPoint.Start → Uninstall)を踏む。
    /// UXML/PanelSettingsを指定しないコード生成フォールバック経路を通るため、
    /// その警告を明示的に宣言する(<c>LogAssert.ignoreFailingMessages</c>はテスト間で
    /// リセットされず後続へ漏れるため使わない).
    /// </remarks>
    [TestFixture]
    public sealed class UIToolkitInstallerTests
    {
        private static readonly Regex NoVisualTreeAssetWarning = new("No VisualTreeAsset assigned");
        private static readonly Regex NoPanelSettingsWarning = new("No PanelSettings assigned");

        private IInstaller _installer;
        private TerminalRuntimeScope _scope;

        /// <summary>このテストのInstallで生成された<see cref="WindowRoot"/>.</summary>
        /// <remarks>
        /// <c>FindFirstObjectByType</c>で拾うと、他のテストや読み込み済みシーンが持つ別の
        /// インスタンスを掴みうる。その場合、生成の検証は<b>別物で成功し</b>、破棄の検証は
        /// <b>残っている別物で失敗する</b>。Installの前後の差分から自分が作ったものを特定する.
        /// </remarks>
        private WindowRoot _windowRoot;

        /// <summary>各テスト前に<see cref="UIToolkitInstaller"/>を用意する.</summary>
        [SetUp]
        public void SetUp()
        {
            _installer = new UIToolkitInstaller();
            _windowRoot = null;
        }

        /// <summary>各テスト後にScopeを破棄する.</summary>
        [TearDown]
        public void TearDown()
        {
            if (_scope == null) return;

            _installer.Uninstall(_scope);
            _scope = null;
        }

        private void InstallAndStart()
        {
            var before = new HashSet<WindowRoot>(FindAllWindowRoots());

            // UXML/PanelSettings未指定のフォールバック経路を通るため、先に警告を宣言しておく。
            // LogAssert.Expectは順序も検証するため、実際に出る順(PanelSettings → VisualTreeAsset)に合わせる.
            LogAssert.Expect(LogType.Warning, NoPanelSettingsWarning);
            LogAssert.Expect(LogType.Warning, NoVisualTreeAssetWarning);

            _scope = _installer.Install();
            _scope.EntryPoint.Start();

            // 差分は1件であるべき。複数あれば重複生成であり、先頭だけを見ると見逃す.
            var created = FindAllWindowRoots().Where(root => !before.Contains(root)).ToArray();

            Assert.That(created.Length, Is.EqualTo(1), "Installで生成されたWindowRootが1つではない");

            _windowRoot = created[0];
        }

        private static WindowRoot[] FindAllWindowRoots() =>
            Object.FindObjectsByType<WindowRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        /// <summary>Installにより<see cref="UIDocument"/>と<see cref="WindowRoot"/>が生成されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Install_UIDocumentとWindowRootが生成される()
        {
            InstallAndStart();
            yield return null;

            Assert.That(_windowRoot.GetComponent<UIDocument>(), Is.Not.Null, "UIDocumentが生成されていない");
            Assert.That(_windowRoot.IsInitialized, Is.True, "UI要素の解決に失敗している");
        }

        /// <summary>コード生成フォールバックでもUI要素が揃うことを検証する.</summary>
        [UnityTest]
        public IEnumerator Install_UXML未指定でもコード生成フォールバックで要素が揃う()
        {
            InstallAndStart();
            yield return null;

            Assert.That(_windowRoot.Root, Is.Not.Null);
            Assert.That(_windowRoot.LogScrollView, Is.Not.Null);
            Assert.That(_windowRoot.InputField, Is.Not.Null);
            Assert.That(_windowRoot.SubmitButton, Is.Not.Null);
            Assert.That(_windowRoot.PromptLabel, Is.Not.Null);
            Assert.That(_windowRoot.LauncherOpenButton, Is.Not.Null);
            Assert.That(_windowRoot.LauncherCloseButton, Is.Not.Null);
        }

        /// <summary>
        /// 生成した要素がパネルへ接続されることを検証する.
        /// </summary>
        /// <remarks>
        /// パネルへ接続されていない要素には疑似入力が届かないため、UIToolkit向けテストの前提になる(#127).
        /// </remarks>
        [UnityTest]
        public IEnumerator Install_生成した要素がパネルへ接続される()
        {
            InstallAndStart();
            yield return null;

            Assert.That(_windowRoot.Root.panel, Is.Not.Null, "パネルへ接続されていない");
            Assert.That(_windowRoot.InputField.panel, Is.SameAs(_windowRoot.Root.panel));
        }

        /// <summary>Uninstallで自前生成したGameObjectが破棄されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Uninstall_自前生成したGameObjectが破棄される()
        {
            InstallAndStart();
            yield return null;

            _installer.Uninstall(_scope);
            _scope = null;
            yield return null;

            // Unityのオブジェクトは破棄後もnull比較でtrueになる(偽装null).
            Assert.That(_windowRoot == null, Is.True, "Installで生成したUIDocumentが残っている");
        }
    }
}

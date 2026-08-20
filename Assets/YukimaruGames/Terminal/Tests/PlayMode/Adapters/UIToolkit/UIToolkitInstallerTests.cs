using System.Collections;
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

        /// <summary>各テスト前に<see cref="UIToolkitInstaller"/>を用意する.</summary>
        [SetUp]
        public void SetUp() => _installer = new UIToolkitInstaller();

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
            // UXML/PanelSettings未指定のフォールバック経路を通るため、先に警告を宣言しておく。
            // LogAssert.Expectは順序も検証するため、実際に出る順(PanelSettings → VisualTreeAsset)に合わせる.
            LogAssert.Expect(LogType.Warning, NoPanelSettingsWarning);
            LogAssert.Expect(LogType.Warning, NoVisualTreeAssetWarning);

            _scope = _installer.Install();
            _scope.EntryPoint.Start();
        }

        /// <summary>Installにより<see cref="UIDocument"/>と<see cref="WindowRoot"/>が生成されることを検証する.</summary>
        [UnityTest]
        public IEnumerator Install_UIDocumentとWindowRootが生成される()
        {
            InstallAndStart();
            yield return null;

            var windowRoot = Object.FindFirstObjectByType<WindowRoot>();

            Assert.That(windowRoot, Is.Not.Null, "WindowRootが生成されていない");
            Assert.That(windowRoot.GetComponent<UIDocument>(), Is.Not.Null, "UIDocumentが生成されていない");
            Assert.That(windowRoot.IsInitialized, Is.True, "UI要素の解決に失敗している");
        }

        /// <summary>コード生成フォールバックでもUI要素が揃うことを検証する.</summary>
        [UnityTest]
        public IEnumerator Install_UXML未指定でもコード生成フォールバックで要素が揃う()
        {
            InstallAndStart();
            yield return null;

            var windowRoot = Object.FindFirstObjectByType<WindowRoot>();

            Assert.That(windowRoot.Root, Is.Not.Null);
            Assert.That(windowRoot.LogScrollView, Is.Not.Null);
            Assert.That(windowRoot.InputField, Is.Not.Null);
            Assert.That(windowRoot.SubmitButton, Is.Not.Null);
            Assert.That(windowRoot.PromptLabel, Is.Not.Null);
            Assert.That(windowRoot.LauncherOpenButton, Is.Not.Null);
            Assert.That(windowRoot.LauncherCloseButton, Is.Not.Null);
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

            var windowRoot = Object.FindFirstObjectByType<WindowRoot>();

            Assert.That(windowRoot.Root.panel, Is.Not.Null, "パネルへ接続されていない");
            Assert.That(windowRoot.InputField.panel, Is.SameAs(windowRoot.Root.panel));
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

            Assert.That(Object.FindFirstObjectByType<WindowRoot>(), Is.Null, "UIDocumentが残っている");
        }
    }
}

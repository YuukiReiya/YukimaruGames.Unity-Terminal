using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using YukimaruGames.Terminal.Adapters.CommandLine;

namespace YukimaruGames.Terminal.Tests.EditMode.Adapters.CommandLine
{
    /// <summary>
    /// セッションディレクトリがシンボリックリンクかどうかの判定を検証する.
    /// </summary>
    /// <remarks>
    /// 一時ディレクトリのパスを先回りして推測され、シンボリックリンクを仕込まれていた場合、
    /// 中継スクリプトやセッショントークンをリンク先へ書き込んでしまう(#119)。
    /// <para>
    /// <c>FileSystemInfo.LinkTarget</c>はUnityのランタイムに存在しないため、属性
    /// (<see cref="FileAttributes.ReparsePoint"/>)で判定している。この前提が崩れると
    /// 検証が素通りするため、実際にリンクを作って確かめる.
    /// </para>
    /// </remarks>
    [TestFixture]
    public sealed class RelayScriptDirectoryTests
    {
        private string _root;

        /// <summary>テスト用の作業ディレクトリを用意する.</summary>
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "yukimaru_terminal_test_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        /// <summary>テストが作ったディレクトリとリンクを片付ける.</summary>
        [TearDown]
        public void TearDown()
        {
            // リンクを先に外す(再帰削除でリンク先の中身まで消さないため).
            var link = LinkPath;
            if (CommandLineRelayScriptWriter.IsSymbolicLink(link)) Directory.Delete(link);

            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        private string LinkPath => Path.Combine(_root, "link");

        /// <summary>通常のディレクトリはリンクと判定されないことを検証します.</summary>
        [Test]
        public void IsSymbolicLink_通常のディレクトリは偽()
        {
            var directory = Path.Combine(_root, "plain");
            Directory.CreateDirectory(directory);

            Assert.That(CommandLineRelayScriptWriter.IsSymbolicLink(directory), Is.False);
        }

        /// <summary>存在しないパスでも例外にならないことを検証します.</summary>
        [Test]
        public void IsSymbolicLink_存在しないパスは偽()
        {
            Assert.That(CommandLineRelayScriptWriter.IsSymbolicLink(Path.Combine(_root, "missing")), Is.False);
        }

        /// <summary>
        /// ディレクトリへのシンボリックリンクを検出できることを検証します.
        /// </summary>
        /// <remarks>
        /// リンクの作成APIがUnityのランタイムに無いため、<c>ln -s</c>で作る
        /// (Windowsではリンク作成に管理者権限が要る場合があるため、このテストはUnix系のみ).
        /// </remarks>
        [Test]
        [Platform(Include = "MacOSX,Linux")]
        public void IsSymbolicLink_ディレクトリへのリンクは真()
        {
            var target = Path.Combine(_root, "target");
            Directory.CreateDirectory(target);

            CreateSymbolicLink(target, LinkPath);

            Assert.That(Directory.Exists(LinkPath), Is.True, "前提: リンクを作成できていること");
            Assert.That(CommandLineRelayScriptWriter.IsSymbolicLink(LinkPath), Is.True);
        }

        private static void CreateSymbolicLink(string target, string link)
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/ln",
                Arguments = $"-s \"{target}\" \"{link}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            process?.WaitForExit();
        }
    }
}

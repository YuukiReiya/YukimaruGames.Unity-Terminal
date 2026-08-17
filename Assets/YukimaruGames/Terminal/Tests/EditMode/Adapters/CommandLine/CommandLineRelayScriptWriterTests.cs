using System;
using System.IO;
using NUnit.Framework;
using YukimaruGames.Terminal.Adapters.CommandLine;

namespace YukimaruGames.Terminal.Tests.EditMode.Adapters.CommandLine
{
    /// <summary>
    /// 書き出される中継スクリプトが、起動直後の打鍵で表示が壊れない構造になっていることを検証する.
    /// </summary>
    /// <remarks>
    /// スクリプトはbash/PowerShellのテキストであり、実行して確かめるのはテストの範囲外のため、
    /// 崩れると不具合が再発する<b>構造上の条件</b>だけを見る。
    /// 具体的には次の2点で、いずれも#158の原因に直接対応する。
    /// <list type="bullet">
    /// <item>端末をrawモードへ切り替えるより前に何も出力しないこと(出力中の打鍵を端末がエコーしてしまう)</item>
    /// <item>入力行の描画が、消した文字数を数えるバックスペースではなく行の引き直しであること</item>
    /// </list>
    /// </remarks>
    [TestFixture]
    public sealed class CommandLineRelayScriptWriterTests
    {
        private string _macScript;

        /// <summary>実際に書き出されるスクリプトを読み込む(定数ではなく成果物を対象にする).</summary>
        [SetUp]
        public void SetUp()
        {
            _macScript = File.ReadAllText(CommandLineRelayScriptWriter.WriteMacRelayScript());
        }

        /// <summary>テストが作ったセッションディレクトリを片付ける.</summary>
        [TearDown]
        public void TearDown()
        {
            CommandLineRelayScriptWriter.DeleteSessionDirectory();
        }

        /// <summary>
        /// 端末をrawモードへ切り替えるまで、画面へ何も出力しないことを検証する.
        /// </summary>
        /// <remarks>
        /// ここが逆転すると、切り替え前に打たれた文字を端末が自前でエコーし、
        /// スクリプトの入力バッファには無いのに画面へ残る(バックスペースでも消せない)状態になる.
        /// </remarks>
        [Test]
        public void MacRelayScript_端末をrawモードにするまで出力しない()
        {
            var rawMode = _macScript.IndexOf("stty raw -echo", StringComparison.Ordinal);
            var firstOutput = FirstOutputIndex(_macScript);

            Assert.That(rawMode, Is.GreaterThanOrEqualTo(0), "rawモードへの切り替えが見当たらない");
            Assert.That(rawMode, Is.LessThan(firstOutput), "rawモードへ切り替える前に画面へ出力している");
        }

        /// <summary>
        /// 入力行の描画に、消した文字数ぶんのバックスペースを使っていないことを検証する.
        /// </summary>
        /// <remarks>
        /// バックスペース方式は、スクリプトが把握していない文字が行に混ざると復帰できない。
        /// 行頭へ戻して消してから引き直す方式であれば、画面は常にバッファと一致する.
        /// </remarks>
        [Test]
        public void MacRelayScript_入力行を行ごと引き直す()
        {
            Assert.That(_macScript, Does.Contain(@"printf '\r\033[K%s%s'"), "行の引き直しが見当たらない");
            Assert.That(_macScript, Does.Not.Contain(@"printf '\b \b'"), "バックスペースで消す描画が残っている");
        }

        /// <summary>
        /// rawモード中の早期終了メッセージが、改行だけで終わっていないことを検証する.
        /// </summary>
        /// <remarks>
        /// rawモードでは<c>\n</c>だけでは行頭へ戻らないため、CRを伴わせないと表示が階段状に崩れる.
        /// </remarks>
        [Test]
        public void MacRelayScript_早期終了メッセージがCRを伴う()
        {
            Assert.That(_macScript, Does.Not.Match(@"(?m)^\s*echo "), "rawモード下でechoを使っている");
        }

        /// <summary>
        /// 画面へ文字を出す最初の位置を返す.
        /// </summary>
        /// <remarks>
        /// 端末へ出るのは<c>printf</c>と<c>echo</c>。<c>printf</c>のうち、ソケット(fd3)へ書くものと
        /// コメント行は画面出力ではないため除く.
        /// </remarks>
        private static int FirstOutputIndex(string script)
        {
            var lines = script.Split('\n');
            var offset = 0;

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                var isOutput = (trimmed.StartsWith("printf ", StringComparison.Ordinal)
                                || trimmed.StartsWith("echo ", StringComparison.Ordinal))
                               && !trimmed.Contains(">&3");

                if (isOutput) return offset;

                offset += line.Length + 1;
            }

            return script.Length;
        }
    }
}

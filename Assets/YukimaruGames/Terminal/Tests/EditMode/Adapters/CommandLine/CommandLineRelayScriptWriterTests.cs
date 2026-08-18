using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using YukimaruGames.Terminal.Adapters.CommandLine;

namespace YukimaruGames.Terminal.Tests.EditMode.Adapters.CommandLine
{
    /// <summary>
    /// 書き出される中継スクリプトが、起動直後の打鍵で表示が壊れない構造になっていることを検証する.
    /// </summary>
    /// <remarks>
    /// スクリプトはbashのテキストであり、実行して確かめるのはテストの範囲外のため、
    /// 崩れると不具合が再発する<b>構造上の条件</b>だけを見る。いずれも#158の原因に直接対応する。
    /// <list type="bullet">
    /// <item>端末をrawモードへ切り替えるより前に何も出力しないこと(出力中の打鍵を端末がエコーしてしまう)</item>
    /// <item>入力行の描画が、消した文字数を数えるバックスペースではなく行の引き直しであること</item>
    /// <item>折り返した行を消せるよう、直前に使った行数を持っていること</item>
    /// </list>
    /// </remarks>
    [TestFixture]
    public sealed class CommandLineRelayScriptWriterTests
    {
        private const string RawModeCommand = "stty raw -echo";
        private const string ConnectCommand = "exec 3<>";

        private string[] _macScriptLines;
        private string _macScript;

        /// <summary>実際に書き出されるスクリプトを読み込む(定数ではなく成果物を対象にする).</summary>
        [SetUp]
        public void SetUp()
        {
            _macScript = File.ReadAllText(CommandLineRelayScriptWriter.WriteMacRelayScript());
            _macScriptLines = _macScript.Split('\n');
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
        /// スクリプトの入力バッファには無いのに画面へ残る(バックスペースでも消せない)状態になる。
        /// <para>
        /// コメント中の記述に引っかからないよう、<b>実行される行</b>だけを対象にする.
        /// </para>
        /// </remarks>
        [Test]
        public void MacRelayScript_端末をrawモードにするまで出力しない()
        {
            var rawMode = IndexOfLine(line => line.StartsWith(RawModeCommand, StringComparison.Ordinal));
            var firstOutput = IndexOfLine(IsScreenOutput);

            Assert.That(rawMode, Is.GreaterThanOrEqualTo(0), "実行されるrawモード切り替えが見当たらない");
            Assert.That(firstOutput, Is.GreaterThanOrEqualTo(0), "画面出力が1つも見当たらない(テストの前提が崩れている)");
            Assert.That(rawMode, Is.LessThan(firstOutput), "rawモードへ切り替える前に画面へ出力している");
        }

        /// <summary>
        /// 入力行の描画に、消した文字数ぶんのバックスペースを使っていないことを検証する.
        /// </summary>
        /// <remarks>
        /// バックスペース方式は、スクリプトが把握していない文字が行に混ざると復帰できない.
        /// </remarks>
        [Test]
        public void MacRelayScript_入力行を行ごと引き直す()
        {
            Assert.That(_macScript, Does.Contain(@"printf '\r\033[J%s'"), "行の引き直しが見当たらない");
            Assert.That(_macScript, Does.Not.Contain(@"printf '\b \b'"), "バックスペースで消す描画が残っている");
        }

        /// <summary>
        /// 折り返した入力行をまとめて消せる作りになっていることを検証する.
        /// </summary>
        /// <remarks>
        /// 入力が端末幅を超えると表示は複数の物理行にまたがる。現在行だけを消すと前の行が残るため、
        /// 直前に使った行数を持ち、その先頭まで戻ってから消す必要がある.
        /// </remarks>
        [Test]
        public void MacRelayScript_折り返した行を戻ってから消す()
        {
            Assert.That(_macScript, Does.Contain("LAST_ROWS"), "直前に使った行数を持っていない");
            Assert.That(_macScript, Does.Contain(@"printf '\033[%dA'"), "折り返しの先頭行へ戻る処理が見当たらない");
            Assert.That(_macScript, Does.Contain("stty size"), "端末幅の取得が見当たらない");
        }

        /// <summary>
        /// rawモード中に出す早期終了メッセージが、必ずCR+LFで終わることを検証する.
        /// </summary>
        /// <remarks>
        /// rawモードでは<c>\n</c>だけでは行頭へ戻らないため、CRを伴わせないと表示が階段状に崩れる。
        /// 接続前(=rawモード確定後)に画面へ出す行を対象にする.
        /// </remarks>
        [Test]
        public void MacRelayScript_接続前の画面出力がCRLFで終わる()
        {
            var connect = IndexOfLine(line => line.StartsWith(ConnectCommand, StringComparison.Ordinal));
            Assert.That(connect, Is.GreaterThanOrEqualTo(0), "接続処理が見当たらない(テストの前提が崩れている)");

            var formats = _macScriptLines
                .Take(connect)
                .Where(IsScreenOutput)
                .Select(line => line.Trim())
                .ToArray();

            Assert.That(formats, Is.Not.Empty, "接続前の画面出力が1つも無い(テストの前提が崩れている)");

            foreach (var line in formats)
            {
                Assert.That(line, Does.StartWith("printf "), $"rawモード下でprintf以外の出力を使っている: {line}");
                Assert.That(FormatOf(line), Does.EndWith(@"\r\n"), $"CRを伴わずに改行している: {line}");
            }
        }

        /// <summary>条件に合う最初の行の位置を返す(見つからなければ-1).</summary>
        private int IndexOfLine(Func<string, bool> predicate)
        {
            for (var i = 0; i < _macScriptLines.Length; i++)
            {
                if (predicate(_macScriptLines[i].Trim())) return i;
            }

            return -1;
        }

        /// <summary>
        /// その行が画面へ文字を出すか.
        /// </summary>
        /// <remarks>
        /// 端末へ出るのは<c>printf</c>と<c>echo</c>。ソケット(fd3)への書き込みとコメントは除く.
        /// </remarks>
        private static bool IsScreenOutput(string line)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("#", StringComparison.Ordinal)) return false;
            if (trimmed.Contains(">&3")) return false;

            return trimmed.StartsWith("printf ", StringComparison.Ordinal)
                   || trimmed.StartsWith("echo ", StringComparison.Ordinal);
        }

        /// <summary><c>printf 'FORMAT' ...</c>の書式部分を取り出す.</summary>
        private static string FormatOf(string line)
        {
            var open = line.IndexOf('\'');
            if (open < 0) return line;

            var close = line.IndexOf('\'', open + 1);

            return close < 0 ? line : line.Substring(open + 1, close - open - 1);
        }
    }
}

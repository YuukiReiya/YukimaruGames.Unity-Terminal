using NUnit.Framework;
using YukimaruGames.Terminal.Adapters.CommandLine;

namespace YukimaruGames.Terminal.Tests.EditMode.Adapters.CommandLine
{
    /// <summary>
    /// <see cref="RichTextAnsiConverter"/>によるリッチテキストタグからANSIエスケープへの変換を検証する.
    /// </summary>
    /// <remarks>
    /// 期待値の可読性のため、テスト内ではエスケープ文字を<see cref="Esc"/>として組み立てる.
    /// </remarks>
    [TestFixture]
    public sealed class RichTextAnsiConverterTests
    {
        private const char Escape = (char)27;
        private static string Esc(string body) => Escape + body;

        /// <summary>色タグがtruecolorのSGRへ変換され、閉じタグで既定色へ戻ることを検証します.</summary>
        [Test]
        public void Convert_ColorTag_BecomesTrueColorSequence()
        {
            var actual = RichTextAnsiConverter.Convert("<color=#a6e22e>ok</color>");

            Assert.That(actual, Is.EqualTo(Esc("[38;5;149m") + "ok" + Esc("[39m") + Esc("[0m")));
        }

        /// <summary>3桁の16進指定が各桁の複製として展開されることを検証します.</summary>
        [Test]
        public void Convert_ShortHexColor_ExpandsEachDigit()
        {
            var actual = RichTextAnsiConverter.Convert("<color=#f00>x</color>");

            Assert.That(actual, Does.StartWith(Esc("[38;5;196m")));
        }

        /// <summary>色名指定が解釈されることを検証します.</summary>
        [Test]
        public void Convert_NamedColor_IsResolved()
        {
            var actual = RichTextAnsiConverter.Convert("<color=red>x</color>");

            Assert.That(actual, Does.StartWith(Esc("[38;5;196m")));
        }

        /// <summary>
        /// 入れ子の閉じタグで、既定色ではなく外側の色へ戻ることを検証します.
        /// </summary>
        /// <remarks>
        /// ANSIには入れ子の概念が無いため、色のスタックを持って外側の色を再送出する必要がある。
        /// ここが壊れると、内側のタグを閉じた瞬間に色が失われる.
        /// </remarks>
        [Test]
        public void Convert_NestedColorTags_RestoresOuterColor()
        {
            var actual = RichTextAnsiConverter.Convert("<color=red>A<color=blue>B</color>C</color>");

            var red = Esc("[38;5;196m");
            var blue = Esc("[38;5;21m");

            Assert.That(actual, Is.EqualTo(red + "A" + blue + "B" + red + "C" + Esc("[39m") + Esc("[0m")));
        }

        /// <summary>太字・斜体が対応するSGRへ変換されることを検証します.</summary>
        [Test]
        public void Convert_BoldAndItalic_BecomeSgrSequences()
        {
            var actual = RichTextAnsiConverter.Convert("<b>a</b><i>b</i>");

            Assert.That(actual, Is.EqualTo(
                Esc("[1m") + "a" + Esc("[22m") + Esc("[3m") + "b" + Esc("[23m") + Esc("[0m")));
        }

        /// <summary>ANSIに対応物が無いタグは、本文を残してタグだけ取り除かれることを検証します.</summary>
        [Test]
        public void Convert_UnsupportedTag_IsDroppedButKeepsBody([Values("size=20", "material=1", "quad size=5")] string tag)
        {
            var name = tag.Split('=', ' ')[0];

            var actual = RichTextAnsiConverter.Convert($"<{tag}>body</{name}>");

            Assert.That(actual, Is.EqualTo("body"));
        }

        /// <summary>
        /// 認識できないタグがそのまま出力されることを検証します.
        /// </summary>
        /// <remarks>
        /// Unityのリッチテキストが未知のタグを文字として描画するのに合わせる。これにより
        /// 例外メッセージ中の山括弧が各バックエンドと同じ見た目になる.
        /// </remarks>
        [Test]
        public void Convert_UnknownTag_IsLeftAsIs(
            [Values(
                "Cannot convert List<int> to IEnumerable<string>",
                "at Foo.<Bar>b__0 ()",
                "<>c__DisplayClass1_0",
                "a < b && c > d")]
            string text)
        {
            Assert.That(RichTextAnsiConverter.Convert(text), Is.EqualTo(text));
        }

        /// <summary>タグを含まない場合は入力がそのまま返ることを検証します.</summary>
        [Test]
        public void Convert_WithoutTags_ReturnsInput()
        {
            Assert.That(RichTextAnsiConverter.Convert("no tags here"), Is.EqualTo("no tags here"));
        }

        /// <summary>閉じられていない山括弧が本文として扱われることを検証します.</summary>
        [Test]
        public void Convert_UnterminatedBracket_IsTreatedAsBody()
        {
            Assert.That(RichTextAnsiConverter.Convert("value < 10"), Is.EqualTo("value < 10"));
        }

        /// <summary>色を無効化した場合、エスケープを出さずタグだけ取り除くことを検証します.</summary>
        [Test]
        public void Convert_WhenNotColored_StripsTagsWithoutEscapes()
        {
            var actual = RichTextAnsiConverter.Convert("<color=red>A<b>B</b></color>", colored: false);

            Assert.That(actual, Is.EqualTo("AB"));
        }

        /// <summary><c>null</c>や空文字列が空文字列になることを検証します.</summary>
        [Test]
        public void Convert_NullOrEmpty_ReturnsEmpty([Values(null, "")] string text)
        {
            Assert.That(RichTextAnsiConverter.Convert(text), Is.Empty);
        }
    }
}

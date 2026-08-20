using NUnit.Framework;
using UnityEngine;
using YukimaruGames.Terminal.Composition;

namespace YukimaruGames.Terminal.Tests.EditMode.Composition
{
    /// <summary>
    /// 画面サイズに応じたフォントサイズの算出を検証する.
    /// </summary>
    /// <remarks>
    /// ウィンドウは画面サイズに対する比率で開くため、フォントサイズを固定のままにすると
    /// 解像度によって1画面に入る行数が変わる。拡縮を有効にした場合に「基準解像度での見え方」が
    /// 保たれること、無効な場合は設定値がそのまま使われることを固定する.
    /// </remarks>
    [TestFixture]
    public sealed class ThemeBinderFontSizeTests
    {
        private const int FontSize = 55;
        private const int ReferenceHeight = 1080;

        /// <summary>拡縮が無効なら、画面サイズに関わらず設定値がそのまま返ることを検証します.</summary>
        [Test]
        public void ResolveFontSize_拡縮が無効なら設定値をそのまま返す([Values(540, 1080, 2160)] int screenHeight)
        {
            var theme = new StubTheme { ScaleFontWithScreen = false };

            Assert.That(ThemeBinder.ResolveFontSize(theme, screenHeight), Is.EqualTo(FontSize));
        }

        /// <summary>
        /// 基準解像度ちょうどのときは、拡縮が有効でも設定値と一致することを検証します.
        /// </summary>
        /// <remarks>
        /// 「基準解像度での大きさ」という設定値の意味が崩れていないことの確認.
        /// </remarks>
        [Test]
        public void ResolveFontSize_基準解像度では設定値と一致する()
        {
            var theme = new StubTheme { ScaleFontWithScreen = true };

            Assert.That(ThemeBinder.ResolveFontSize(theme, ReferenceHeight), Is.EqualTo(FontSize));
        }

        /// <summary>画面高さの比率どおりに拡縮されることを検証します.</summary>
        [Test]
        public void ResolveFontSize_画面高さの比率で拡縮する(
            [Values(540, 720, 2160)] int screenHeight)
        {
            var theme = new StubTheme { ScaleFontWithScreen = true };
            var expected = Mathf.RoundToInt(FontSize * (screenHeight / (float)ReferenceHeight));

            Assert.That(ThemeBinder.ResolveFontSize(theme, screenHeight), Is.EqualTo(expected));
        }

        /// <summary>
        /// 極端に小さい画面でも0にならないことを検証します.
        /// </summary>
        /// <remarks>
        /// 0は「描画しない」と区別がつかず、原因の追いにくい不具合になるため下限を1にしている.
        /// </remarks>
        [Test]
        public void ResolveFontSize_極端に小さい画面でも1を下回らない()
        {
            var theme = new StubTheme { ScaleFontWithScreen = true };

            Assert.That(ThemeBinder.ResolveFontSize(theme, 1), Is.GreaterThanOrEqualTo(1));
        }

        /// <summary>
        /// 画面サイズや基準解像度が取得できない場合は拡縮しないことを検証します.
        /// </summary>
        /// <remarks>
        /// 初期化順の都合で0が渡ることがある。そこで0除算や極端な値を出さず、設定値へ退避する.
        /// </remarks>
        [Test]
        public void ResolveFontSize_高さが取得できない場合は拡縮しない()
        {
            var scaled = new StubTheme { ScaleFontWithScreen = true };

            Assert.That(ThemeBinder.ResolveFontSize(scaled, 0), Is.EqualTo(FontSize), "画面高さが0");

            var noReference = new StubTheme { ScaleFontWithScreen = true, Reference = new Vector2Int(1920, 0) };

            Assert.That(ThemeBinder.ResolveFontSize(noReference, 540), Is.EqualTo(FontSize), "基準解像度の高さが0");
        }

        /// <summary>テーマが無い場合に例外となることを検証します(値の供給元のため必須).</summary>
        [Test]
        public void ResolveFontSize_テーマがnullなら例外()
        {
            Assert.That(() => ThemeBinder.ResolveFontSize(null, 1080), Throws.ArgumentNullException);
        }

        /// <summary>フォントサイズと拡縮設定だけを持つテスト用のテーマ.</summary>
        private sealed class StubTheme : ITerminalTheme
        {
            public bool ScaleFontWithScreen { get; set; }
            public Vector2Int Reference { get; set; } = new(1920, ReferenceHeight);

            public Vector2Int ReferenceResolution => Reference;
            public int FontSize => ThemeBinderFontSizeTests.FontSize;

            public Font Font => null;
            public Color BackgroundColor => default;
            public Color MessageColor => default;
            public Color EntryColor => default;
            public Color WarningColor => default;
            public Color ErrorColor => default;
            public Color AssertColor => default;
            public Color ExceptionColor => default;
            public Color SystemColor => default;
            public Color InputColor => default;
            public Color CaretColor => default;
            public Color SelectionColor => default;
            public Color PromptColor => default;
            public Color ExecuteButtonColor => default;
            public Color ButtonColor => default;
            public Color CopyButtonColor => default;
            public float CursorFlashSpeed => default;
        }
    }
}

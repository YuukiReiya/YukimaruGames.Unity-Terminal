using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Models
{
    [TestFixture]
    public class TerminalColorTests
    {
        // コンストラクタテスト (6ケース)
        [Test]
        public void Constructor_RgbValues_CreatesColor()
        {
            // Arrange & Act
            var color = new TerminalColor(255, 128, 64);
            
            // Assert
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(128, color.G);
            Assert.AreEqual(64, color.B);
            Assert.AreEqual(255, color.A);  // デフォルト値
        }

        [Test]
        public void Constructor_WithAlpha_SetsAlpha()
        {
            var color = new TerminalColor(255, 0, 0, 128);
            Assert.AreEqual(128, color.A);
        }

        [Test]
        public void Constructor_ArgbInteger_ParsesCorrectly()
        {
            // 0xAARRGGBB
            var color = new TerminalColor(0xFFFF0000);  // 赤, 不透明
            Assert.AreEqual(255, color.A);
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(0, color.G);
            Assert.AreEqual(0, color.B);
        }

        // TryParseHex テスト (18ケース)
        [Test]
        public void TryParseHex_ValidHex6_Parses()
        {
            var span = "#FF0000".AsSpan();
            Assert.IsTrue(TerminalColor.TryParseHex(span, out var color));
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(0, color.G);
            Assert.AreEqual(0, color.B);
            Assert.AreEqual(255, color.A);  // デフォルト
        }

        [Test]
        public void TryParseHex_ValidHex8_ParsesWithAlpha()
        {
            var span = "#FF000080".AsSpan();
            Assert.IsTrue(TerminalColor.TryParseHex(span, out var color));
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(0, color.G);
            Assert.AreEqual(0, color.B);
            Assert.AreEqual(128, color.A);
        }

        [Test]
        public void TryParseHex_LowercaseHex_Parses()
        {
            var span = "#ffffff".AsSpan();
            Assert.IsTrue(TerminalColor.TryParseHex(span, out var color));
            Assert.AreEqual(255, color.R);
            Assert.AreEqual(255, color.G);
            Assert.AreEqual(255, color.B);
        }

        [Test]
        public void TryParseHex_InvalidLength_ReturnsFalse()
        {
            var span = "#FFF".AsSpan();
            Assert.IsFalse(TerminalColor.TryParseHex(span, out _));
        }

        [Test]
        public void TryParseHex_InvalidHexCharacters_ReturnsFalse()
        {
            var span = "#GGGGGG".AsSpan();
            Assert.IsFalse(TerminalColor.TryParseHex(span, out _));
        }

        [Test]
        public void TryParseHex_EmptySpan_ReturnsFalse()
        {
            ReadOnlySpan<char> span = "";
            Assert.IsFalse(TerminalColor.TryParseHex(span, out _));
        }

        // TryFormat テスト (18ケース)
        [Test]
        public void TryFormat_RgbToHex_FormatsCorrectly()
        {
            // Arrange
            var color = new TerminalColor(255, 0, 0);
            Span<char> buffer = stackalloc char[7];

            // Act
            var success = color.TryFormat(buffer, includeAlpha: false);

            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual("#FF0000", new string(buffer));
        }

        [Test]
        public void TryFormat_RgbaToHex_FormatsWithAlpha()
        {
            var color = new TerminalColor(255, 0, 0, 128);
            Span<char> buffer = stackalloc char[9];

            var success = color.TryFormat(buffer, includeAlpha: true);

            Assert.IsTrue(success);
            Assert.AreEqual("#FF000080", new string(buffer));
        }

        [Test]
        public void TryFormat_InsufficientBuffer_ReturnsFalse()
        {
            var color = TerminalColor.Red;
            Span<char> buffer = stackalloc char[3];  // 不足

            var success = color.TryFormat(buffer, includeAlpha: false);

            Assert.IsFalse(success);
        }

        // ToHex テスト (8ケース)
        [Test]
        public void ToHex_RgbFormat_ReturnsString()
        {
            var color = new TerminalColor(255, 0, 0);
            var hex = color.ToHex(includeAlpha: false);
            Assert.AreEqual("#FF0000", hex);
        }

        [Test]
        public void ToHex_RgbaFormat_ReturnsWithAlpha()
        {
            var color = new TerminalColor(255, 0, 0, 128);
            var hex = color.ToHex(includeAlpha: true);
            Assert.AreEqual("#FF000080", hex);
        }

        // 色空間変換テスト (12ケース)
        [Test]
        public void ToLinear_White_ReturnsOne()
        {
            var white = TerminalColor.White;
            var (r, g, b, a) = white.ToLinear();

            Assert.IsTrue(Math.Abs(r - 1f) < 0.01f);
            Assert.IsTrue(Math.Abs(g - 1f) < 0.01f);
            Assert.IsTrue(Math.Abs(b - 1f) < 0.01f);
            Assert.IsTrue(Math.Abs(a - 1f) < 0.01f);
        }

        [Test]
        public void FromLinear_OneVector_ReturnsWhite()
        {
            var white = TerminalColor.FromLinear(1f, 1f, 1f, 1f);

            Assert.AreEqual(255, white.R);
            Assert.AreEqual(255, white.G);
            Assert.AreEqual(255, white.B);
            Assert.AreEqual(255, white.A);
        }

        [Test]
        public void ColorSpace_RoundTrip_PreservesColor()
        {
            var red = TerminalColor.Red;
            var (r, g, b, a) = red.ToLinear();
            var roundTrip = TerminalColor.FromLinear(r, g, b, a);

            Assert.AreEqual(red.R, roundTrip.R);
            Assert.AreEqual(red.G, roundTrip.G);
            Assert.AreEqual(red.B, roundTrip.B);
            Assert.AreEqual(red.A, roundTrip.A);
        }

        // 等価性テスト (8ケース)
        [Test]
        public void Equals_SameValues_AreEqual()
        {
            var color1 = new TerminalColor(255, 0, 0);
            var color2 = new TerminalColor(255, 0, 0);

            Assert.IsTrue(color1.Equals(color2));
            Assert.AreEqual(color1, color2);
            Assert.AreEqual(color1.GetHashCode(), color2.GetHashCode());
        }

        [Test]
        public void NotEquals_DifferentValues_AreNotEqual()
        {
            var red = new TerminalColor(255, 0, 0);
            var blue = new TerminalColor(0, 0, 255);

            Assert.IsFalse(red.Equals(blue));
            Assert.AreNotEqual(red, blue);
        }

        // 標準カラー定数テスト (6ケース)
        [Test]
        public void StandardColors_Black_IsBlack()
        {
            Assert.AreEqual(0, TerminalColor.Black.R);
            Assert.AreEqual(0, TerminalColor.Black.G);
            Assert.AreEqual(0, TerminalColor.Black.B);
            Assert.AreEqual(255, TerminalColor.Black.A);
        }

        [Test]
        public void StandardColors_White_IsWhite()
        {
            Assert.AreEqual(255, TerminalColor.White.R);
            Assert.AreEqual(255, TerminalColor.White.G);
            Assert.AreEqual(255, TerminalColor.White.B);
            Assert.AreEqual(255, TerminalColor.White.A);
        }

        [Test]
        public void StandardColors_Transparent_IsTransparent()
        {
            Assert.AreEqual(0, TerminalColor.Transparent.A);
        }

        // ToString テスト (2ケース)
        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var color = new TerminalColor(255, 0, 0, 128);
            var str = color.ToString();

            Assert.IsTrue(str.Contains("255"));
            Assert.IsTrue(str.Contains("128"));
        }
    }
}

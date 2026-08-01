using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Models
{
    [TestFixture]
    public sealed class TerminalRectTests
    {
        [Test]
        public void Constructor_SetsFields()
        {
            // Arrange & Act
            var rect = new TerminalRect(1f, 2f, 3f, 4f);

            // Assert
            Assert.AreEqual(1f, rect.X);
            Assert.AreEqual(2f, rect.Y);
            Assert.AreEqual(3f, rect.Width);
            Assert.AreEqual(4f, rect.Height);
        }

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            var a = new TerminalRect(1f, 2f, 3f, 4f);
            var b = new TerminalRect(1f, 2f, 3f, 4f);

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Equals_DifferentValues_ReturnsFalse()
        {
            var a = new TerminalRect(1f, 2f, 3f, 4f);
            var b = new TerminalRect(1f, 2f, 3f, 5f);

            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }

        [Test]
        public void GetHashCode_SameValues_AreEqual()
        {
            var a = new TerminalRect(1f, 2f, 3f, 4f);
            var b = new TerminalRect(1f, 2f, 3f, 4f);

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var rect = new TerminalRect(1f, 2f, 3f, 4f);
            Assert.AreEqual("TerminalRect(1, 2, 3, 4)", rect.ToString());
        }

        [Test]
        public void Zero_HasAllFieldsZero()
        {
            var rect = TerminalRect.Zero;
            Assert.AreEqual(new TerminalRect(0f, 0f, 0f, 0f), rect);
        }
    }
}

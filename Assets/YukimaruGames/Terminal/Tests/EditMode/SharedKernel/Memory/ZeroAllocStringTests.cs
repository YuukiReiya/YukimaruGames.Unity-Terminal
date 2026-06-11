using System;
using System.Reflection;
using NUnit.Framework;
using YukimaruGames.Terminal.SharedKernel.Interfaces;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.SharedKernel.Memory;
using Assert = NUnit.Framework.Assert;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Models
{
    /// <summary>
    /// ZeroAllocStringクラスの単体テスト.
    /// </summary>
    [TestFixture]
    public class ZeroAllocStringTests
    {
        private IPool<char[]> _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new ArrayPool<char>(defaultCapacity: 256);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ValidPool_CreatesInstance()
        {
            // Act
            using var buffer = new ZeroAllocString(_pool);

            // Assert
            Assert.IsNotNull(buffer);
            Assert.AreEqual(0, buffer.Length);
            Assert.IsTrue(buffer.IsEmpty);
        }

        [Test]
        public void Constructor_NullPool_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                var buffer = new ZeroAllocString(null);
            });
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var buffer = new ZeroAllocString(_pool, initialCapacity: -1);
            });
        }

        [Test]
        public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var buffer = new ZeroAllocString(_pool, initialCapacity: 0);
            });
        }

        #endregion

        #region Append Tests

        [Test]
        public void Append_String_AddsTextToBuffer()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);

            // Act
            buffer.Append("Hello");

            // Assert
            Assert.AreEqual("Hello", buffer.ToAllocatedString());
            Assert.AreEqual(5, buffer.Length);
            Assert.IsFalse(buffer.IsEmpty);
        }

        [Test]
        public void Append_MultipleStrings_ConcatenatesCorrectly()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);

            // Act
            buffer.Append("Hello");
            buffer.Append(" ");
            buffer.Append("World");

            // Assert
            Assert.AreEqual("Hello World", buffer.ToAllocatedString());
            Assert.AreEqual(11, buffer.Length);
        }

        [Test]
        public void Append_EmptyString_DoesNotChangeBuffer()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");

            // Act
            buffer.Append("");

            // Assert
            Assert.AreEqual("Test", buffer.ToAllocatedString());
            Assert.AreEqual(4, buffer.Length);
        }

        [Test]
        public void Append_Span_AddsTextToBuffer()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            ReadOnlySpan<char> span = "Hello".AsSpan();

            // Act
            buffer.Append(span);

            // Assert
            Assert.AreEqual("Hello", buffer.ToAllocatedString());
            Assert.AreEqual(5, buffer.Length);
        }

        [Test]
        public void Append_NullString_ThrowsArgumentNullException()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => { buffer.Append((string)null); });
        }

        #endregion

        #region Replace Tests

        [Test]
        public void Replace_AtBeginning_ReplacesCorrectly()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hello World");

            // Act
            buffer.Replace(0, 5, "Hi".AsSpan());

            // Assert
            Assert.AreEqual("Hi World", buffer.ToAllocatedString());
            Assert.AreEqual(8, buffer.Length);
        }

        [Test]
        public void Replace_AtEnd_ReplacesCorrectly()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hello World");

            // Act
            buffer.Replace(6, 5, "Unity".AsSpan());

            // Assert
            Assert.AreEqual("Hello Unity", buffer.ToAllocatedString());
            Assert.AreEqual(11, buffer.Length);
        }

        [Test]
        public void Replace_InMiddle_ReplacesCorrectly()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hello World");

            // Act
            buffer.Replace(6, 5, "C#".AsSpan());

            // Assert
            Assert.AreEqual("Hello C#", buffer.ToAllocatedString());
            Assert.AreEqual(8, buffer.Length);
        }

        [Test]
        public void Replace_WithLongerText_ExpandsBuffer()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hi");

            // Act
            buffer.Replace(0, 2, "Hello World".AsSpan());

            // Assert
            Assert.AreEqual("Hello World", buffer.ToAllocatedString());
            Assert.AreEqual(11, buffer.Length);
        }

        [Test]
        public void Replace_NegativeIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.Replace(-1, 1, "X".AsSpan()); });
        }

        [Test]
        public void Replace_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.Replace(0, -1, "X".AsSpan()); });
        }

        [Test]
        public void Replace_IndexOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => { buffer.Replace(10, 1, "X".AsSpan()); });
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsLength()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hello World");

            // Act
            buffer.Clear();

            // Assert
            Assert.AreEqual(0, buffer.Length);
            Assert.IsTrue(buffer.IsEmpty);
        }

        [Test]
        public void Clear_WithClearBuffer_ZeroesMemory()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Secret");

            // リフレクションを利用した private フィールド (_buffer) の参照取得
            var fi2Buffer = typeof(ZeroAllocString).GetField("_buffer", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            buffer.Clear(clearBuffer: true);

            // Assert
            Assert.AreEqual(0, buffer.Length);

            var internalBuffer = (char[])fi2Buffer!.GetValue(buffer);
            for (var i = 0; i < internalBuffer.Length; i++)
            {
                Assert.AreEqual('\0', internalBuffer[i], $"Index {i} should be zero-cleared.");
            }
        }

        #endregion

        #region AsSpan/AsMemory Tests

        [Test]
        public void AsSpan_ReturnsCorrectData()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hello");

            // Act
            ReadOnlySpan<char> span = buffer.AsSpan();

            // Assert
            Assert.AreEqual(5, span.Length);
            Assert.AreEqual("Hello", new string(span));
        }

        [Test]
        public void AsMemory_ReturnsCorrectData()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("World");

            // Act
            ReadOnlyMemory<char> memory = buffer.AsMemory();

            // Assert
            Assert.AreEqual(5, memory.Length);
            Assert.AreEqual("World", new string(memory.Span));
        }

        [Test]
        public void AsSpan_EmptyBuffer_ReturnsEmpty()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);

            // Act
            ReadOnlySpan<char> span = buffer.AsSpan();

            // Assert
            Assert.AreEqual(0, span.Length);
            Assert.IsTrue(span.IsEmpty);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");

            // Act & Assert
            buffer.Release();
            Assert.DoesNotThrow(() => buffer.Release());
        }

        [Test]
        public void AsSpan_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");
            buffer.Release();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
            {
                var _ = buffer.AsSpan();
            });
        }

        [Test]
        public void AsMemory_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");
            buffer.Release();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
            {
                var _ = buffer.AsMemory();
            });
        }

        [Test]
        public void Append_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var buffer = new ZeroAllocString(_pool);
            buffer.Release();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => { buffer.Append("Test"); });
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_ReturnsWarningMessage()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Test");

            // Act
            string result = buffer.ToString();

            // Assert
            Assert.AreEqual("[ZeroAllocString: Use ToAllocatedString() for UI caching]", result);
        }

        [Test]
        public void ToAllocatedString_ReturnsCorrectString()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);
            buffer.Append("Hello World");

            // Act
            string result = buffer.ToAllocatedString();

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [Test]
        public void ToAllocatedString_EmptyBuffer_ReturnsEmptyString()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool);

            // Act
            string result = buffer.ToAllocatedString();

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        #endregion

        #region Capacity Expansion Tests

        [Test]
        public void Append_ExceedsInitialCapacity_ExpandsAutomatically()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool, initialCapacity: 8);
            string longText = new string('A', 100);

            // Act
            buffer.Append(longText);

            // Assert
            Assert.AreEqual(100, buffer.Length);
            Assert.AreEqual(longText, buffer.ToAllocatedString());
            Assert.GreaterOrEqual(buffer.Capacity, 100);
        }

        [Test]
        public void Replace_CausesExpansion_WorksCorrectly()
        {
            // Arrange
            using var buffer = new ZeroAllocString(_pool, initialCapacity: 8);
            buffer.Append("Hi");
            string longText = new string('B', 100);

            // Act
            buffer.Replace(0, 2, longText.AsSpan());

            // Assert
            Assert.AreEqual(100, buffer.Length);
            Assert.AreEqual(longText, buffer.ToAllocatedString());
        }

        #endregion
    }
}
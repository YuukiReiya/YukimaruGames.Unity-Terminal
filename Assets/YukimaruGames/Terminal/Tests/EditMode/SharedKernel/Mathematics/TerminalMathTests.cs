using System;
using NUnit.Framework;
using YukimaruGames.Terminal.SharedKernel.Mathematics;

namespace YukimaruGames.Terminal.Tests.EditMode.SharedKernel.Mathematics
{
    /// <summary>
    /// ターミナル計算クラスの単体テスト
    /// </summary>
    [TestFixture]
    public sealed class TerminalMathTests
    {
        // ─── Clamp(float) Tests ────────────────────────────────────────────────────
        
        [Test]
        public void Clamp_Float_ValueWithinRange_ReturnsSameValue()
        {
            // Arrange
            float value = 5f;
            float min = 0f;
            float max = 10f;

            // Act
            float result = TerminalMath.Clamp(value, min, max);

            // Assert
            Assert.AreEqual(5f, result);
        }

        [Test]
        public void Clamp_Float_ValueBelowMin_ReturnsMin()
        {
            // Arrange
            float value = -5f;
            float min = 0f;
            float max = 10f;

            // Act
            float result = TerminalMath.Clamp(value, min, max);

            // Assert
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Clamp_Float_ValueAboveMax_ReturnsMax()
        {
            // Arrange
            float value = 15f;
            float min = 0f;
            float max = 10f;

            // Act
            float result = TerminalMath.Clamp(value, min, max);

            // Assert
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Clamp_Float_ValueEqualsMin_ReturnsMin()
        {
            // Arrange
            float value = 0f;
            float min = 0f;
            float max = 10f;

            // Act
            float result = TerminalMath.Clamp(value, min, max);

            // Assert
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Clamp_Float_ValueEqualsMax_ReturnsMax()
        {
            // Arrange
            float value = 10f;
            float min = 0f;
            float max = 10f;

            // Act
            float result = TerminalMath.Clamp(value, min, max);

            // Assert
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Clamp_Float_MinGreaterThanMax_ThrowsExpectedException()
        {
            float value = 5f;
            float invalidMin = 10.0f;
            float invalidMax = 0.0f;
            // Math.ClampがArgumentOutOfRangeExceptionをスローすることを期待
            Assert.Throws<ArgumentException>(() =>
            {
                TerminalMath.Clamp(value, invalidMin, invalidMax);
            });
        }

        // ─── Clamp(int) Tests ────────────────────────────────────────────────────
        [Test]
        public void Clamp_Int_ValueWithinRange_ReturnsSameValue()
        {
            Assert.AreEqual(5, TerminalMath.Clamp(5, 0, 10));
        }

        [Test]
        public void Clamp_Int_ValueBelowMin_ReturnsMin()
        {
            Assert.AreEqual(0, TerminalMath.Clamp(-5, 0, 10));
        }

        [Test]
        public void Clamp_Int_ValueAboveMax_ReturnsMax()
        {
            Assert.AreEqual(10, TerminalMath.Clamp(15, 0, 10));
        }

        // ─── Clamp01 Tests ────────────────────────────────────────────────────

        [Test]
        public void Clamp01_ValueWithinRange_ReturnsSameValue()
        {
            Assert.AreEqual(0.5f, TerminalMath.Clamp01(0.5f));
        }

        [Test]
        public void Clamp01_ValueBelowZero_ReturnsZero()
        {
            Assert.AreEqual(0f, TerminalMath.Clamp01(-0.5f));
        }

        [Test]
        public void Clamp01_ValueAboveOne_ReturnsOne()
        {
            Assert.AreEqual(1f, TerminalMath.Clamp01(1.5f));
        }

        [Test]
        public void Clamp01_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, TerminalMath.Clamp01(0f));
        }

        [Test]
        public void Clamp01_One_ReturnsOne()
        {
            Assert.AreEqual(1f, TerminalMath.Clamp01(1f));
        }
        
        [Test]
        public void Clamp_Int_MinGreaterThanMax_ThrowsExpectedException()
        {
            int value = 5;
            int invalidMin = 10;
            int invalidMax = 0;
            // Math.ClampがArgumentOutOfRangeExceptionをスローすることを期待
            Assert.Throws<ArgumentException>(() =>
            {
                TerminalMath.Clamp(value, invalidMin, invalidMax);
            });
        }
        
        // ─── Lerp Tests ────────────────────────────────────────────────────
        
        [Test]
        public void Lerp_TEqualsZero_ReturnsA()
        {
            float result = TerminalMath.Lerp(0f, 10f, 0f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Lerp_TEqualsOne_ReturnsB()
        {
            float result = TerminalMath.Lerp(0f, 10f, 1f);
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Lerp_TEqualsHalf_ReturnsMidpoint()
        {
            float result = TerminalMath.Lerp(0f, 10f, 0.5f);
            Assert.AreEqual(5f, result);
        }

        [Test]
        public void Lerp_TAboveOne_ClampedToB()
        {
            float result = TerminalMath.Lerp(0f, 10f, 2f);
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Lerp_TBelowZero_ClampedToA()
        {
            float result = TerminalMath.Lerp(0f, 10f, -1f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Lerp_NegativeRange_InterpolatesCorrectly()
        {
            float result = TerminalMath.Lerp(-10f, 10f, 0.5f);
            Assert.AreEqual(0f, result);
        }

        // ─── LerpUnclamped Tests ────────────────────────────────────────────────────

        [Test]
        public void LerpUnclamped_TEqualsZero_ReturnsA()
        {
            float result = TerminalMath.LerpUnclamped(0f, 10f, 0f);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void LerpUnclamped_TEqualsOne_ReturnsB()
        {
            float result = TerminalMath.LerpUnclamped(0f, 10f, 1f);
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void LerpUnclamped_TEqualsTwo_ExtrapolatesCorrectly()
        {
            float result = TerminalMath.LerpUnclamped(0f, 10f, 2f);
            Assert.AreEqual(20f, result);
        }

        [Test]
        public void LerpUnclamped_TNegative_ExtrapolatesCorrectly()
        {
            float result = TerminalMath.LerpUnclamped(0f, 10f, -1f);
            Assert.AreEqual(-10f, result);
        }

        // ─── Min/Max Tests ────────────────────────────────────────────────────

        [Test]
        public void Min_Float_ReturnsSmaller()
        {
            Assert.AreEqual(3f, TerminalMath.Min(5f, 3f));
            Assert.AreEqual(3f, TerminalMath.Min(3f, 5f));
        }

        [Test]
        public void Max_Float_ReturnsLarger()
        {
            Assert.AreEqual(5f, TerminalMath.Max(5f, 3f));
            Assert.AreEqual(5f, TerminalMath.Max(3f, 5f));
        }

        [Test]
        public void Min_Int_ReturnsSmaller()
        {
            Assert.AreEqual(3, TerminalMath.Min(5, 3));
            Assert.AreEqual(3, TerminalMath.Min(3, 5));
        }

        [Test]
        public void Max_Int_ReturnsLarger()
        {
            Assert.AreEqual(5, TerminalMath.Max(5, 3));
            Assert.AreEqual(5, TerminalMath.Max(3, 5));
        }

        // ─── Approximately Tests ────────────────────────────────────────────────────

        [Test]
        public void Approximately_EqualValues_ReturnsTrue()
        {
            Assert.IsTrue(TerminalMath.Approximately(1f, 1f));
        }

        [Test]
        public void Approximately_ZeroAndTinyEpsilonValue_ReturnsTrue()
        {
            // float.Epsilon * 8 未満の絶対差はゼロ近傍として等しいとみなされる
            Assert.IsTrue(TerminalMath.Approximately(0f, float.Epsilon * 4f));
        }

        [Test]
        public void Approximately_LargeValuesWithSmallRelativeDifference_ReturnsTrue()
        {
            // 相対誤差判定のため、大きな値では許容差も比例して大きくなる
            Assert.IsTrue(TerminalMath.Approximately(1000000f, 1000000.05f));
        }

        [Test]
        public void Approximately_ClearlyDifferentValues_ReturnsFalse()
        {
            Assert.IsFalse(TerminalMath.Approximately(0f, 1f));
        }

        [Test]
        public void Approximately_SmallAbsoluteButLargeRelativeDifference_ReturnsFalse()
        {
            // 0付近では相対誤差がほぼ0になるため、1e-8程度の差でも等しいとはみなされない
            Assert.IsFalse(TerminalMath.Approximately(0f, 1e-8f));
        }

        // ─── SmoothStep Tests ────────────────────────────────────────────────────

        [Test]
        public void SmoothStep_TEqualsZero_ReturnsFrom()
        {
            Assert.AreEqual(0f, TerminalMath.SmoothStep(0f, 10f, 0f));
        }

        [Test]
        public void SmoothStep_TEqualsOne_ReturnsTo()
        {
            Assert.AreEqual(10f, TerminalMath.SmoothStep(0f, 10f, 1f));
        }

        [Test]
        public void SmoothStep_TEqualsHalf_ReturnsMidpoint()
        {
            Assert.AreEqual(5f, TerminalMath.SmoothStep(0f, 10f, 0.5f));
        }

        [Test]
        public void SmoothStep_TAboveOne_ClampedToTo()
        {
            Assert.AreEqual(10f, TerminalMath.SmoothStep(0f, 10f, 2f));
        }

        [Test]
        public void SmoothStep_TBelowZero_ClampedToFrom()
        {
            Assert.AreEqual(0f, TerminalMath.SmoothStep(0f, 10f, -1f));
        }
    }
}
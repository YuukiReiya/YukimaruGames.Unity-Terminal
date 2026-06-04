using System;
using System.Runtime.CompilerServices;

namespace YukimaruGames.Terminal.SharedKernel.Mathematics
{
    /// <summary>
    /// Unity非依存の数学ヘルパー関数.
    /// <para>
    /// UnityEngine.Mathfの代替として、SharedKernelで使用可能な数学関数を提供します。
    /// </para>
    /// </summary>
    public static class TerminalMath
    {
        /// <summary>
        /// 浮動小数点値を指定範囲内にクランプする.
        /// </summary>
        /// <param name="value">クランプする値</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>min以上max以下にクランプされた値</returns>
        /// <example>
        /// <code>
        /// float clamped = TerminalMath.Clamp(5.5f, 0f, 10f);  // 5.5
        /// float clamped = TerminalMath.Clamp(-5f, 0f, 10f);   // 0.0
        /// float clamped = TerminalMath.Clamp(15f, 0f, 10f);   // 10.0
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);

        /// <summary>
        /// 整数値を指定範囲内にクランプする.
        /// </summary>
        /// <param name="value">クランプする値</param>
        /// <param name="min">最小値</param>
        /// <param name="max">最大値</param>
        /// <returns>min以上max以下にクランプされた値</returns>
        /// <example>
        /// <code>
        /// int clamped = TerminalMath.Clamp(5, 0, 10);   // 5
        /// int clamped = TerminalMath.Clamp(-5, 0, 10);  // 0
        /// int clamped = TerminalMath.Clamp(15, 0, 10);  // 10
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);

        /// <summary>
        /// 値を0〜1の範囲にクランプする.
        /// </summary>
        /// <param name="value">クランプする値</param>
        /// <returns>0以上1以下にクランプされた値</returns>
        /// <example>
        /// <code>
        /// float clamped = TerminalMath.Clamp01(0.5f);  // 0.5
        /// float clamped = TerminalMath.Clamp01(-0.5f); // 0.0
        /// float clamped = TerminalMath.Clamp01(1.5f);  // 1.0
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

        /// <summary>
        /// 線形補間（0〜1の範囲にクランプ）.
        /// </summary>
        /// <param name="a">開始値</param>
        /// <param name="b">終了値</param>
        /// <param name="t">補間パラメータ（0〜1にクランプされる）</param>
        /// <returns>aとbの間の補間値</returns>
        /// <remarks>
        /// <para>
        /// tが0の場合はa、1の場合はbを返します。
        /// tが0未満または1を超える場合、自動的に0〜1にクランプされます。
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// float result = TerminalMath.Lerp(0f, 10f, 0f);   // 0.0
        /// float result = TerminalMath.Lerp(0f, 10f, 0.5f); // 5.0
        /// float result = TerminalMath.Lerp(0f, 10f, 1f);   // 10.0
        /// float result = TerminalMath.Lerp(0f, 10f, 2f);   // 10.0 (クランプ)
        /// </code>
        /// </example>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float a, float b, float t) => LerpUnclamped(a, b, Clamp01(t));

        /// <summary>
        /// クランプなし線形補間（外挿可能）.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;

        /// <summary>
        /// 最小値を返す.
        /// </summary>
        /// <param name="a">比較値1</param>
        /// <param name="b">比較値2</param>
        /// <returns>aとbのうち小さい方</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Min(float a, float b) => Math.Min(a, b);

        /// <inheritdoc cref="Min(float,float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int a, int b) => Math.Min(a, b);
        
        /// <summary>
        /// 最大値を返す.
        /// </summary>
        /// <param name="a">比較値1</param>
        /// <param name="b">比較値2</param>
        /// <returns>aとbのうち大きい方</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(float a, float b) => Math.Max(a, b);
        
        /// <inheritdoc cref="Max(float,float)"/> 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Max(int a, int b) => Math.Max(a, b);
    }
}

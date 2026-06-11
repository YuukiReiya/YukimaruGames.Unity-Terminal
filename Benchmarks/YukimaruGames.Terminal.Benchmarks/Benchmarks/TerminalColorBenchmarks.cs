using BenchmarkDotNet.Attributes;
using YukimaruGames.Terminal.Domain.Models;

namespace YukimaruGames.Terminal.Benchmarks
{
    /// <summary>
    /// TerminalColor パフォーマンスベンチマーク.
    /// 
    /// 実行方法:
    /// dotnet run -c Release --project YukimaruGames.Terminal.Benchmarks
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 5)]
    [RankColumn]
    public class TerminalColorBenchmarks
    {
        // ─── セットアップ ─────────────────────────────────────────────────

        private string _hexColorString;
        private ReadOnlyMemory<char> _hexColorMemory;
        private char[] _formatBuffer;
        private TerminalColor _testColor;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _hexColorString = "#FF8040FF";
            _hexColorMemory = _hexColorString.AsMemory();
            _formatBuffer = new char[9];
            _testColor = new TerminalColor(255, 128, 64, 255);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            Array.Clear(_formatBuffer, 0, _formatBuffer.Length);
        }

        // ─── TryParseHex ベンチマーク ────────────────────────────────────

        /// <summary>
        /// TryParseHex: ReadOnlySpan&lt;char&gt; 入力（ゼロアロケーション）
        /// </summary>
        [Benchmark]
        public bool TryParseHex_ReadOnlySpan()
        {
            var span = _hexColorString.AsSpan();
            return TerminalColor.TryParseHex(span, out _);
        }

        /// <summary>
        /// TryParseHex: ダイレクト文字列入力（型推論）
        /// </summary>
        [Benchmark]
        public bool TryParseHex_DirectString()
        {
            return TerminalColor.TryParseHex(_hexColorString.AsSpan(), out _);
        }

        /// <summary>
        /// TryParseHex: ループ内での複数回パース（実際の使用シーン）
        /// </summary>
        [Benchmark]
        public int TryParseHex_MultipleIterations()
        {
            int count = 0;
            var span = _hexColorString.AsSpan();
            
            for (int i = 0; i < 100; i++)
            {
                if (TerminalColor.TryParseHex(span, out _))
                    count++;
            }
            
            return count;
        }

        // ─── TryFormat ベンチマーク ────────────────────────────────────────

        /// <summary>
        /// TryFormat: RGB形式 (7文字, #RRGGBB)
        /// </summary>
        [Benchmark]
        public bool TryFormat_RgbFormat()
        {
            Span<char> buffer = stackalloc char[7];
            return _testColor.TryFormat(buffer, includeAlpha: false);
        }

        /// <summary>
        /// TryFormat: RGBA形式 (9文字, #RRGGBBAA)
        /// </summary>
        [Benchmark]
        public bool TryFormat_RgbaFormat()
        {
            Span<char> buffer = stackalloc char[9];
            return _testColor.TryFormat(buffer, includeAlpha: true);
        }

        /// <summary>
        /// TryFormat: スタック割り当て（最適化版）
        /// </summary>
        [Benchmark]
        public int TryFormat_StackAllocLoop()
        {
            int count = 0;
            
            for (int i = 0; i < 100; i++)
            {
                Span<char> buffer = stackalloc char[9];
                if (_testColor.TryFormat(buffer, includeAlpha: true))
                    count++;
            }
            
            return count;
        }

        // ─── ToHex ベンチマーク ──────────────────────────────────────────

        /// <summary>
        /// ToHex: RGB形式（文字列生成）
        /// 
        /// 注記: この操作は GC Alloc を発生させます（string 割り当て）
        /// ホットパスではない操作として想定されています。
        /// </summary>
        [Benchmark]
        public string ToHex_RgbFormat()
        {
            return _testColor.ToHex(includeAlpha: false);
        }

        /// <summary>
        /// ToHex: RGBA形式（文字列生成）
        /// </summary>
        [Benchmark]
        public string ToHex_RgbaFormat()
        {
            return _testColor.ToHex(includeAlpha: true);
        }

        // ─── 色空間変換 ベンチマーク ──────────────────────────────────────

        /// <summary>
        /// ToLinear: Gamma → Linear 変換
        /// 
        /// 負荷: MathF.Pow x 3
        /// </summary>
        [Benchmark]
        public (float, float, float, float) ToLinear_Conversion()
        {
            return _testColor.ToLinear();
        }

        /// <summary>
        /// FromLinear: Linear → Gamma 変換
        /// 
        /// 負荷: MathF.Pow x 3
        /// </summary>
        [Benchmark]
        public TerminalColor FromLinear_Conversion()
        {
            var (r, g, b, a) = _testColor.ToLinear();
            return TerminalColor.FromLinear(r, g, b, a);
        }

        /// <summary>
        /// ColorSpace_RoundTrip: ラウンドトリップ変換
        /// 
        /// Gamma → Linear → Gamma の往復変換で
        /// カラー値が保持されるか検証
        /// </summary>
        [Benchmark]
        public bool ColorSpace_RoundTrip()
        {
            var original = _testColor;
            var (r, g, b, a) = original.ToLinear();
            var roundTrip = TerminalColor.FromLinear(r, g, b, a);
            
            return original == roundTrip;
        }

        // ─── メモリ効率 ベンチマーク ──────────────────────────────────────

        /// <summary>
        /// Constructor_DefaultAlpha: デフォルト A=255
        /// </summary>
        [Benchmark]
        public TerminalColor Constructor_DefaultAlpha()
        {
            return new TerminalColor(255, 128, 64);
        }

        /// <summary>
        /// Constructor_WithAlpha: 明示的な A 値
        /// </summary>
        [Benchmark]
        public TerminalColor Constructor_WithAlpha()
        {
            return new TerminalColor(255, 128, 64, 200);
        }

        /// <summary>
        /// Constructor_FromArgb: ARGB整数値から生成
        /// </summary>
        [Benchmark]
        public TerminalColor Constructor_FromArgb()
        {
            return new TerminalColor(0xFFF8804F);
        }

        /// <summary>
        /// ToArgb: カラーを ARGB 整数値に変換
        /// </summary>
        [Benchmark]
        public uint ToArgb_Conversion()
        {
            return _testColor.ToArgb();
        }

        // ─── 等価性判定 ベンチマーク ─────────────────────────────────────

        /// <summary>
        /// Equals_Method: IEquatable&lt;T&gt;.Equals メソッド
        /// </summary>
        [Benchmark]
        public bool Equals_Method()
        {
            var color1 = new TerminalColor(255, 128, 64);
            var color2 = new TerminalColor(255, 128, 64);
            return color1.Equals(color2);
        }

        /// <summary>
        /// EqualityOperator: == 演算子オーバーロード
        /// </summary>
        [Benchmark]
        public bool EqualityOperator()
        {
            var color1 = new TerminalColor(255, 128, 64);
            var color2 = new TerminalColor(255, 128, 64);
            return color1 == color2;
        }

        /// <summary>
        /// GetHashCode: ハッシュコード生成
        /// </summary>
        [Benchmark]
        public int GetHashCode_Generation()
        {
            return _testColor.GetHashCode();
        }

        // ─── 実際の使用シーン ベンチマーク ───────────────────────────────

        /// <summary>
        /// RealWorldScenario_LogFormatting: ログ出力フォーマット
        /// 
        /// シーン: "[#FF8040] [Info] Message" のようなログ出力
        /// </summary>
        [Benchmark]
        public string RealWorldScenario_LogFormatting()
        {
            // パース
            var logLine = "[#FF8040] [Info] Test message".AsSpan();
            if (!TerminalColor.TryParseHex(logLine.Slice(1, 7), out var color))
                return null;

            // フォーマット
            return color.ToHex(includeAlpha: false);
        }

        /// <summary>
        /// RealWorldScenario_ColorPalette: カラーパレット初期化
        /// 
        /// シーン: 複数のカラーをパースして HashMap に格納
        /// </summary>
        [Benchmark]
        public int RealWorldScenario_ColorPalette()
        {
            var colors = new Dictionary<string, TerminalColor>();
            var colorDefs = new[]
            {
                ("#FF0000", "Red"),
                ("#00FF00", "Green"),
                ("#0000FF", "Blue"),
                ("#FFFF00", "Yellow"),
                ("#FF00FF", "Magenta"),
            };

            foreach (var (hex, name) in colorDefs)
            {
                if (TerminalColor.TryParseHex(hex, out var color))
                    colors[name] = color;
            }

            return colors.Count;
        }

        /// <summary>
        /// RealWorldScenario_TerminalRendering: ターミナル描画
        /// 
        /// シーン: 色付けされたテキスト を複数行描画
        /// </summary>
        [Benchmark]
        public int RealWorldScenario_TerminalRendering()
        {
            int lineCount = 0;
            
            for (int i = 0; i < 50; i++)
            {
                // 色をフォーマット
                Span<char> buffer = stackalloc char[9];
                var color = new TerminalColor((byte)(i * 5), (byte)(i * 3), (byte)(i * 7));
                
                if (color.TryFormat(buffer, includeAlpha: false))
                    lineCount++;
            }

            return lineCount;
        }
    }
}
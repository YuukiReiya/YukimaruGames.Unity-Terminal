using System;
using System.Runtime.CompilerServices;
using YukimaruGames.Terminal.SharedKernel.Interfaces;

namespace YukimaruGames.Terminal.Domain.Models
{
    /// <summary>
    /// ターミナルシステムにおける文字データの保持・制御・置換を専門に行うドメインモデル。
    /// </summary>
    public sealed class TerminalStringBuffer : IDisposable
    {
        private readonly IPool<char[]> _pool;
        private readonly int _initialCapacity;
        private char[] _buffer;
        private bool _disposed = false;

        /// <summary>
        /// バッファの容量（確保されている配列のサイズ）.
        /// </summary>
        public int Capacity { get; private set; }
        
        /// <summary>
        /// 現在の文字列の長さ（有効な文字数）.
        /// </summary>
        public int Length { get; private set; }
        
        /// <summary>
        /// バッファが空かどうか.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="pool">文字配列のプール</param>
        /// <param name="initialCapacity">初期容量（デフォルト: 256）</param>
        public TerminalStringBuffer(IPool<char[]> pool, int initialCapacity = 256)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));

            if (initialCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be positive.");
            }
            
            _initialCapacity = initialCapacity;
        }

        /// <summary>
        /// 同期処理用のReadOnlySpan&lt;char&gt;を取得する（最速）.
        /// </summary>
        public ReadOnlySpan<char> AsSpan()
        {
            return _buffer == null || Length <= 0 ?
                ReadOnlySpan<char>.Empty :
                _buffer.AsSpan(0, Length);
        }

        /// <summary>
        /// 非同期処理用のReadOnlyMemory&lt;char&gt;を取得する（async/await対応）.
        /// </summary>
        public ReadOnlyMemory<char> AsMemory()
        {
            return _buffer == null || Length <= 0
                ? ReadOnlyMemory<char>.Empty
                : _buffer.AsMemory(0, Length);
        }

        /// <summary>
        /// 文字列を末尾に追加する.
        /// </summary>
        public void Append(ReadOnlySpan<char> text)
        {
            Replace(Length, 0, text);
        }

        /// <summary>
        /// 文字列を末尾に追加する（string版）.
        /// </summary>
        public void Append(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            
            Append(text.AsSpan());
        }
        
        /// <summary>
        /// 指定位置の文字列を置換する.
        /// </summary>
        /// <param name="index">置換開始位置</param>
        /// <param name="count">削除する文字数</param>
        /// <param name="text">挿入する文字列</param>
        public void Replace(int index, int count, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();
            EnsureAllocated();

            if (index < 0 || Length < index || Length < index + count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), 
                    $"Index {index} with count {count} is out of range (Length: {Length}).");
            }
            
            var newLength = Length - count + text.Length;

            // キャパシティ自動拡張
            if (_buffer!.Length < newLength)
            {
                ExpandCapacity(newLength, index, count, text);
            }
            else
            {
                // バッファ内安全シフト（オーバーラップ対応のCopyToを使用）
                // コピー元と先が重なる（オーバーラップ）可能性があるため、Span.CopyToの内部最適化に任せる
                var tailLength = Length - (index + count);
                if (0 < tailLength && count != text.Length)
                {
                    _buffer.AsSpan(index + count, tailLength)
                        .CopyTo(_buffer.AsSpan(index + text.Length));
                }
            }

            // 置換テキストをバッファに適用
            if (0 < text.Length)
            {
                text.CopyTo(_buffer.AsSpan(index));
            }

            Length = newLength;
        }

        /// <summary>
        /// バッファの内容をクリアする.
        /// </summary>
        /// <param name="clearBuffer">trueの場合、バッファの中身もゼロクリアする（セキュリティ重視）</param>
        public void Clear(bool clearBuffer = false)
        {
            ThrowIfDisposed();
            
            Length = 0;
            
            if (clearBuffer && _buffer != null)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
            }
        }
        
        public void Release()
        {
            ((IDisposable)this).Dispose();
        }
        
        /// <summary>
        /// バッファが未確保なら確保する（Lazy Initialization）.
        /// </summary>
        private void EnsureAllocated()
        {
            if (_buffer != null) return;
 
            var requestedCapacity = RoundUpToPowerOfTwo(_initialCapacity);
            _buffer = _pool.Get(requestedCapacity);
 
            // 実際に取得した配列のサイズをCapacityとして記録
            Capacity = _buffer.Length;
            Length = 0;
        }
        
        /// <summary>
        /// バッファ容量を拡張する（内部処理）.
        /// </summary>
        private void ExpandCapacity(int newLength, int index, int count, ReadOnlySpan<char> text)
        {
            // Capacity は 2のべき乗が保証されているためここで2倍することで1ビットシフトするより たくさんの余剰バッファが確保できる
            var newCapacity = Capacity * 2;
                
            // 倍化して得た余剰バッファを超過する場合は新しい Capacity を満たす2のべき乗サイズを新たな Capacity とする
            if (newCapacity < newLength)
            {
                newCapacity = RoundUpToPowerOfTwo(newLength);
            }
 
            var newBuffer = _pool.Get(newCapacity);
 
            // 書き換え位置より前のデータを退避
            if (index > 0)
            {
                _buffer.AsSpan(0, index).CopyTo(newBuffer);
            }
 
            // 書き換え位置より後のデータを新バッファへ移動
            var tailLength = Length - (index + count);
            if (tailLength > 0)
            {
                _buffer.AsSpan(index + count, tailLength)
                    .CopyTo(newBuffer.AsSpan(index + text.Length));
            }
 
            // 古いバッファの返却
            _pool.Release(_buffer);
 
            // バッファ更新
            _buffer = newBuffer;
            
            // 確保してる Capacity のキャッシュを更新 
            Capacity = _buffer.Length;
        }
        
        /// <summary>
        /// バッファをプールに返却する.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_buffer != null)
            {
                _pool.Release(_buffer);
                _buffer = null;
            }

            Length = 0;
            Capacity = 0;
            _disposed = true;
        }
        
        /// <summary>
        /// stringに変換する（GC Allocが発生する）.
        /// <para>
        /// 表示目的やデバッグ用途にのみ使用すること。
        /// </para>
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ?
                string.Empty :
                new string(_buffer, 0, Length);
        }

        /// <summary>
        /// Dispose済みならエラーをスローする.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TerminalStringBuffer));
            }
        }

        /// <summary>
        /// 指定された数値以上の「最も近い2のべき乗（Power of Two）」の数を計算します。
        /// </summary>
        /// <param name="value">必要な最小のバッファ長（例: 16, 17, 600 など）</param>
        /// <returns>計算された2のべき乗の数（例: 16->16, 17->32, 600->1024）</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int RoundUpToPowerOfTwo(int value)
        {
            // 0以下の場合は、2のべき乗の最小スタートラインである「1」を返す
            if (value <= 0) return 1;

            // =================================================================
            // 【重要】ステップ1: 最初に1を引く（安全弁 / 境界線ガード）
            // =================================================================
            // なぜマイナス1をするのか？
            // -> 入力された数値が「最初からピッタリ2のべき乗（例: 16）」だった場合に、
            //    この後のビット埋め処理で「次のサイズ（32）」へ誤って繰り上がるバグを防ぐため。
            // 
            // 挙動の検証：
            // A) value = 16 (00010000) の場合:
            //    16 - 1 = 15 (00001111) になる。最上位ビットの位置が1つ下に下がる。
            // B) value = 17 (00010001) の場合:
            //    17 - 1 = 16 (00010000) になる。1を引いても最上位ビットの位置は変わらない！
            value--;

            // =================================================================
            // 【重要】ステップ2: ビットの雪崩（なだれ）処理
            // =================================================================
            // 最上位の「1」のビットを右側へ向かって倍々ゲームでコピーしていき、
            // 最上位ビットより右側にあるすべての隙間を「1」で完全に埋め尽くします。

            // 1つ右のビットを「1」にする（合計2ビットが「1」の塊になる）
            // 例: 00010000 -> 00010000 | 00001000 -> 00011000
            value |= value >> 1;

            // 2つ右まで「1」にする（前の行で2連続になった「1」を2マスずらすので、一気に4連続になる）
            // 例: 00011000 -> 00011000 | 00000110 -> 00011110
            value |= value >> 2;

            // 4つ右まで「1」にする（4連続している「1」を4マスずらすので、一気に8連続になる）
            // 例: 00011110 -> 00011110 | 00000001 -> 00011111
            value |= value >> 4;

            // 8つ右まで「1」にする（8連続している「1」を8マスずらすので、一気に16連続になる）
            value |= value >> 8;

            // 16つ右まで「1」にする（16連続している「1」を16マスずらすので、一気に32連続になる）
            // これにより、32ビットの整数（int型）の全領域が綺麗に「1」で染まりきる。
            value |= value >> 16;

            // =================================================================
            // 【重要】ステップ3: 最後に1を足して「2のべき乗」に変身させる
            // =================================================================
            // 右側がすべて「1」で埋まった状態のバイナリ（例: 00011111 = 31）に
            // 最後に「1」を足すと、桁上がりが一気に最上位まで発生し、次の2のべき乗になります。
            // 
            // 最終結果の答え合わせ：
            // A) 元が「16」だった場合:
            //    ステップ1で 15 (00001111) になり、ステップ2でも変化せず 15 (00001111)。
            //    最後に 15 + 1 = 16 となり、元の綺麗な2のべき乗が維持される（大正解）。
            // 
            // B) 元が「17」だった場合:
            //    ステップ1で 16 (00010000) になり、ステップ2で右が埋まり 31 (00011111) になる。
            //    最後に 31 + 1 = 32 となり、次の2のべき乗へ安全に繰り上がる（大正解）。
            value++;

            return value;
        }
    }
}
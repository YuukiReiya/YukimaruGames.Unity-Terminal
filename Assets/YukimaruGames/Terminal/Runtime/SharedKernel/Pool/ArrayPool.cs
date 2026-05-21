#if UNITY_EDITOR
#define DEV_METRICS
using System.Threading;
#endif
using System;
using System.Runtime.CompilerServices;
using YukimaruGames.Terminal.SharedKernel.Interfaces;

namespace YukimaruGames.Terminal.SharedKernel
{
    /// <summary>
    /// System.Buffers.ArrayPool を利用したプリミティブ型の効率化WrapPool
    /// </summary>
    /// <typeparam name="T">プール型</typeparam>
    public sealed class ArrayPool<T> : IPool<T[]>
    {
        private readonly int _defaultCapacity;
        private readonly bool _isReferenceOrContainsReferences;
#if DEV_METRICS
        // =================================================================
        // デバッグ用メトリクス
        // =================================================================
        
        private long _totalRents;
        private long _totalReturns;

        /// <summary>
        /// パフォーマンス計測用：メトリクス情報取得
        /// </summary>
        /// <returns>
        /// Rents : Poolから取り出した回数
        /// Returns : Poolへ戻した回数
        /// </returns>
        public (long Rents, long Returns) GetMetrics()
        {
            return (Interlocked.Read(ref _totalRents), Interlocked.Read(ref _totalReturns));
        }
#endif
        public ArrayPool(int defaultCapacity)
        {
            if (defaultCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultCapacity),
                    $"Default capacity must be greater than 0. Actual: {defaultCapacity}");
            }
            _defaultCapacity = defaultCapacity;

            _isReferenceOrContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>(); 
        }

        /// <inheritdoc/> 
        T[] IPool<T[]>.Get() => ((IPool<T[]>)this).Get(_defaultCapacity);
        
        /// <inheritdoc/>
        T[] IPool<T[]>.Get(int minimumCapacity)
        {
            var size = minimumCapacity <= 0 ?
                _defaultCapacity :
                minimumCapacity;

#if DEV_METRICS
            Interlocked.Increment(ref _totalRents);
#endif
            
            return System.Buffers.ArrayPool<T>.Shared.Rent(size);
        }

        /// <inheritdoc/>
        void IPool<T[]>.Release(T[] item)
        {
            if (item == null) return;

#if DEV_METRICS
            Interlocked.Increment(ref _totalReturns);
#endif
            
            System.Buffers.ArrayPool<T>.Shared.Return(item, clearArray: _isReferenceOrContainsReferences);
        }

        /// <summary>
        /// プール内のリソースをクリアします。
        /// <para>
        /// ⚠️ 注意: System.Buffers.ArrayPool.Sharedを使用しているため、
        /// このメソッドは何も実行しません。
        /// </para>
        /// </summary>
        void IPool<T[]>.Clear() { }
    }
}
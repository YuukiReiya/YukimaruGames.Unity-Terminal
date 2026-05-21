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
            return System.Buffers.ArrayPool<T>.Shared.Rent(size);
        }

        /// <inheritdoc/>
        void IPool<T[]>.Release(T[] item)
        {
            if (item == null) return;
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
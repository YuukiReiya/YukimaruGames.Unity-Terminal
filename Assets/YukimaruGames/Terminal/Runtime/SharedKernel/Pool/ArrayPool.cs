using System;
using System.Runtime.CompilerServices;
using YukimaruGames.Terminal.SharedKernel.Interfaces;

namespace YukimaruGames.Terminal.SharedKernel
{
    /// <summary>
    /// System.Buffers.ArrayPool を利用したプリミティブ型の効率化WrapPool
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ArrayPool<T> : IPool<T[]>
    {
        private readonly int _defaultCapacity;
        private readonly bool _isReferenceOrContainsReferences;
            
        public ArrayPool(int defaultCapacity)
        {
            if (defaultCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defaultCapacity), "The default capacity must be greater than or equal to 1.");
            }
            _defaultCapacity = defaultCapacity;

            _isReferenceOrContainsReferences = RuntimeHelpers.IsReferenceOrContainsReferences<T>(); 
        }

        T[] IPool<T[]>.Get() => ((IPool<T[]>)this).Get(_defaultCapacity);
        T[] IPool<T[]>.Get(int minimumCapacity)
        {
            var size = minimumCapacity <= 0 ?
                _defaultCapacity :
                minimumCapacity;
            return System.Buffers.ArrayPool<T>.Shared.Rent(size);
        }

        void IPool<T[]>.Release(T[] item)
        {
            if (item == null) return;
            System.Buffers.ArrayPool<T>.Shared.Return(item, clearArray: _isReferenceOrContainsReferences);
        }

        void IPool<T[]>.Clear() { }
    }
}

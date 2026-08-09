using UnityEngine.Pool;

namespace YukimaruGames.Terminal.Adapters.IMGUI
{
    /// <summary>
    /// <see cref="LogLineView"/>を再利用するためのオブジェクトプール.
    /// </summary>
    /// <remarks>
    /// スレッドセーフではない。IMGUI描画は<c>OnGUI</c>（メインスレッド）からのみ呼び出されることを前提とし、
    /// 本プールもメインスレッド専用として扱うこと。
    /// </remarks>
    public sealed class LogLinePool
    {
        private const int DefaultCapacity = 32;
        private const int MaxSize = 256;

        private readonly ObjectPool<LogLineView> _pool;

        public LogLinePool()
        {
            _pool = new ObjectPool<LogLineView>(
                createFunc: static () => new LogLineView(),
                actionOnGet: null,
                actionOnRelease: static view => view.Reset(),
                actionOnDestroy: null,
                collectionCheck: true,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxSize);
        }

        /// <summary>プールから<see cref="LogLineView"/>を取得する.</summary>
        public LogLineView Get() => _pool.Get();

        /// <summary>使用済みの<see cref="LogLineView"/>をプールへ返却する.</summary>
        public void Release(LogLineView view) => _pool.Release(view);
    }
}

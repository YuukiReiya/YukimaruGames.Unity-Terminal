namespace YukimaruGames.Terminal.SharedKernel.Interfaces
{
    /// <summary>
    /// 汎用プーリング機構
    /// </summary>
    public interface IPool<T>
    {
        /// <summary>
        /// デフォルトの規定サイズでオブジェクトをプールから取得(確保)します。
        /// </summary>
        T Get();

        /// <summary>
        /// 指定された最小要求サイズを満たすオブジェクトをプールから取得(確保)します。
        /// 呼び出し側(バッファ等)での容量不足による自動拡張の要求に対応します。
        /// </summary>
        /// <param name="minimumCapacity">必要となる最小の長さや容量</param>
        T Get(int minimumCapacity);

        /// <summary>
        /// 使用済みのオブジェクトをプールへ解放・返却し、再利用可能な状態にします。
        /// </summary>
        void Release(T item);

        /// <summary>
        /// プール内にキャッシュされているリソースをすべてクリア(強制解放)します。
        /// </summary>
        void Clear();
    }
}
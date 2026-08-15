namespace YukimaruGames.Terminal.SharedKernel
{
    /// <summary>
    /// 初期化が済んだ後に1回だけ呼ばれる.
    /// </summary>
    /// <remarks>
    /// Unityは全オブジェクトの<c>OnEnable</c>が終わってから<c>Start</c>を呼ぶため、
    /// 「他コンポーネントの初期化完了を前提にできる最初のタイミング」として使う。
    /// <c>Awake</c>から走る<c>Install()</c>の時点では、シーン上の他コンポーネントが
    /// まだ有効化されていない可能性がある(uGUIバックエンドがシーン上の<c>EventSystem</c>を
    /// 見落として重複生成しうる問題の対策。#152)。
    /// <para>
    /// 毎フレーム走る<see cref="IUpdatable"/>でフラグ判定して1回だけ処理する方式は取らない。
    /// 解決後も呼び出しが残り続け、更新ループに無駄が残るため.
    /// </para>
    /// </remarks>
    public interface IStartable
    {
        /// <summary>初期化完了後に1回だけ呼ばれる.</summary>
        void Start();
    }

    /// <summary>
    /// 毎フレーム呼ばれる.
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>毎フレーム呼ばれる.</summary>
        /// <param name="deltaTime">前フレームからの経過秒数.</param>
        void Update(float deltaTime);
    }
}

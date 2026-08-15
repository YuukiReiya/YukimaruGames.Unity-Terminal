using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// アプリケーションのライフサイクルイベント(Update, OnGUI, Dispose)を管理する.
    /// Installerによって構築され、TerminalRuntimeScopeを通じてBootstrapperから呼び出される.
    /// </summary>
    /// <remarks>
    /// Updatablesの駆動はキーボード種別によらず<see cref="Update"/>に統一している。
    /// <see cref="OnGUI"/>はUnityの仕様上1フレームに複数回(Layout/Repaint等)呼ばれるため、
    /// そちらで駆動すると同一フレーム内でキー入力判定が重複実行されてしまう
    /// (Legacy Input選択時にOpen等のアクションが二重発火するリグレッションの原因だった)。
    /// </remarks>
    public sealed class TerminalEntryPoint
    {
        private readonly IReadOnlyList<IStartable> _startables;
        private readonly IReadOnlyList<IUpdatable> _updatables;
        private readonly ITerminalGUI _gui;

        /// <summary>
        /// <see cref="TerminalEntryPoint"/>を構築する.
        /// </summary>
        /// <param name="startables">初期化完了後に1回だけ<see cref="Start"/>から駆動する対象一覧.</param>
        /// <param name="updatables">毎フレーム<see cref="Update"/>から駆動する更新対象一覧.</param>
        /// <param name="gui"><see cref="OnGUI"/>から描画するGUI実装(未使用時はnull許容).</param>
        public TerminalEntryPoint(
            IReadOnlyList<IStartable> startables,
            IReadOnlyList<IUpdatable> updatables,
            ITerminalGUI gui)
        {
            _startables = startables;
            _updatables = updatables;
            _gui = gui;
        }

        /// <summary>
        /// 初期化完了後に1回だけ呼び出し、登録済みの<see cref="IStartable"/>全てを駆動する.
        /// </summary>
        /// <remarks>
        /// Unityは全オブジェクトの<c>OnEnable</c>が終わってから<c>Start</c>を呼ぶため、
        /// ここは他コンポーネントの初期化完了を前提にできる最初のタイミングになる
        /// (<c>Install()</c>が走る<c>Awake</c>では前提にできない).
        /// </remarks>
        public void Start()
        {
            if (_startables == null) return;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _startables.Count; ++i) _startables[i]?.Start();
        }

        /// <summary>
        /// 毎フレーム1回呼び出し、登録済みの<see cref="IUpdatable"/>全てを<see cref="Time.deltaTime"/>で更新する.
        /// </summary>
        public void Update()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _updatables.Count; ++i) _updatables[i]?.Update(Time.deltaTime);
        }

        /// <summary>
        /// UnityのOnGUIコールバックから呼び出し、GUIの描画のみを行う(入力判定は<see cref="Update"/>側が担う).
        /// </summary>
        public void OnGUI()
        {
            _gui?.Render();
        }
    }
}

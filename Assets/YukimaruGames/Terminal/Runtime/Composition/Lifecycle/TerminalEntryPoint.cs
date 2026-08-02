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
        private readonly IReadOnlyList<IUpdatable> _updatables;
        private readonly ITerminalGUI _gui;

        public TerminalEntryPoint(
            IReadOnlyList<IUpdatable> updatables,
            ITerminalGUI gui)
        {
            _updatables = updatables;
            _gui = gui;
        }

        public void Update()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _updatables.Count; ++i) _updatables[i]?.Update(Time.deltaTime);
        }

        public void OnGUI()
        {
            _gui?.Render();
        }
    }
}

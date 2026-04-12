using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// アプリケーションのライフサイクルイベント(Update, OnGUI, Dispose)を管理する.
    /// Installerによって構築され、TerminalRuntimeScopeを通じてBootstrapperから呼び出される.
    /// </summary>
    public sealed class TerminalEntryPoint
    {
        private readonly IReadOnlyList<IUpdatable> _updatables;
        private readonly InputKeyboardType _keyboardType;
        private readonly ITerminalGUI _gui;

        public TerminalEntryPoint(
            IReadOnlyList<IUpdatable> updatables, 
            InputKeyboardType keyboardType,
            ITerminalGUI gui)
        {
            _updatables = updatables;
            _keyboardType = keyboardType;
            _gui = gui;
        }

        public void Update()
        {
            if (_keyboardType is InputKeyboardType.InputSystem)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _updatables.Count; ++i) _updatables[i]?.Update(Time.deltaTime);
            }
        }
        
        public void OnGUI()
        {
            if (_keyboardType is InputKeyboardType.Legacy)
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < _updatables.Count; ++i) _updatables[i]?.Update(Time.deltaTime);
            }
            _gui?.Render();
        }
    }
}

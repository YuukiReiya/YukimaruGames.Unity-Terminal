using System.Collections.Generic;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.UI.View;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// アプリケーションのライフサイクルイベント(Update, OnGUI, Dispose)を管理する.
    /// Installerによって構築され、TerminalRuntimeScopeを通じてBootstrapperから呼び出される.
    /// </summary>
    public class TerminalEntryPoint
    {
        private readonly IReadOnlyList<IUpdatable> _updatables;
        private readonly InputKeyboardType _keyboardType;
        private readonly ITerminalView _view;

        public TerminalEntryPoint(
            IReadOnlyList<IUpdatable> updatables, 
            InputKeyboardType keyboardType,
            ITerminalView view)
        {
            _updatables = updatables;
            _keyboardType = keyboardType;
            _view = view;
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
            _view?.Render();
        }
    }
}

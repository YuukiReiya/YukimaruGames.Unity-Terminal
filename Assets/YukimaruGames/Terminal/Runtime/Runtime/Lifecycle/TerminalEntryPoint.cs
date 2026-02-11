using System;
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
    public class TerminalEntryPoint : IDisposable
    {
        private readonly List<IUpdatable> _updatables;
        private readonly List<IDisposable> _disposables;
        private readonly InputKeyboardType _keyboardType;
        private readonly ITerminalView _view;

        public TerminalEntryPoint(
            List<IUpdatable> updatables, 
            List<IDisposable> disposables,
            InputKeyboardType keyboardType,
            ITerminalView view)
        {
            _updatables = updatables;
            _disposables = disposables;
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

        public void Dispose()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < _disposables.Count; i++)
            {
                _disposables[i]?.Dispose();
            }
            _disposables.Clear();
        }
    }
}

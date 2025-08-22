using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.Presentation;

namespace YukimaruGames.Terminal.UI.View
{

    public sealed class TerminalButtonRenderer
    {
        private readonly IGUIStyleProvider _provider;
        public event Action OnExecuteButton;
        public TerminalButtonRenderer(IGUIStyleProvider provider)
        {
            _provider = provider;
        }
        public void Render()
        {
            if (GUILayout.Button("| exec", _provider.GetStyle(),GUILayout.ExpandWidth(false)))
            {
                OnExecuteButton?.Invoke();
            }
        }

    }
}

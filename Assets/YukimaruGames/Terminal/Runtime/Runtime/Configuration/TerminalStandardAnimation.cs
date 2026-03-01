using System;
using UnityEngine;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime
{
    /// <inheritdoc />
    /// <remarks>
    /// ターミナルウィンドウのアニメーション・レイアウト設定のデフォルト実装です。
    /// <see cref="ITerminalAnimation"/> の基本ルールに従います。
    /// </remarks>
    [Serializable]
    public sealed class TerminalStandardAnimation : ITerminalAnimation
    {
        [SerializeField] private TerminalState _bootupWindowState = TerminalState.Close;
        [SerializeField] private TerminalAnchor _anchor = TerminalAnchor.Top;
        [SerializeField] private TerminalWindowStyle _windowStyle = TerminalWindowStyle.Compact;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _compactScale = 0.35f;

        public TerminalState BootupWindowState => _bootupWindowState;
        public TerminalAnchor Anchor => _anchor;
        public TerminalWindowStyle WindowStyle => _windowStyle;
        public float Duration => _duration;
        public float CompactScale => _compactScale;
    }
}

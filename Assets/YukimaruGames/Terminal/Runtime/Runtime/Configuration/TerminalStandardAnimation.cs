using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Window;

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
        [SerializeField] private WindowState _bootupWindowState = WindowState.Close;
        [SerializeField] private WindowAnchor _anchor = WindowAnchor.Top;
        [SerializeField] private WindowStyle _windowStyle = WindowStyle.Compact;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _compactScale = 0.35f;

        public WindowState BootupWindowState => _bootupWindowState;
        public WindowAnchor Anchor => _anchor;
        public WindowStyle WindowStyle => _windowStyle;
        public float Duration => _duration;
        public float CompactScale => _compactScale;
    }
}

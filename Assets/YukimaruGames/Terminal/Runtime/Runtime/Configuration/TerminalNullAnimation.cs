using System;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// 最小限の設定値を持つ Null Object パターン実装.
    /// ユーザーが意図的に Animation を null にした場合のフォールバック先.
    /// </summary>
    [Serializable, HideInTypeMenu]
    public sealed class TerminalNullAnimation : ITerminalAnimation
    {
        public TerminalState BootupWindowState => TerminalState.Close;
        public TerminalAnchor Anchor => TerminalAnchor.Top;
        public TerminalWindowStyle WindowStyle => TerminalWindowStyle.Full;
        public float Duration => 0f;
        public float CompactScale => 0f;
    }
}

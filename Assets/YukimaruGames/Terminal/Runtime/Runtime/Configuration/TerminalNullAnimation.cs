using System;
using YukimaruGames.Terminal.Presentation.Models.Window;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
{
    /// <summary>
    /// 最小限の設定値を持つ Null Object パターン実装.
    /// ユーザーが意図的に Animation を null にした場合のフォールバック先.
    /// </summary>
    [Serializable, HideInTypeMenu]
    public sealed class TerminalNullAnimation : ITerminalAnimation
    {
        public WindowState BootupWindowState => WindowState.Close;
        public WindowAnchor Anchor => WindowAnchor.Top;
        public WindowStyle WindowStyle => WindowStyle.Full;
        public float Duration => 0f;
        public float CompactScale => 0f;
    }
}

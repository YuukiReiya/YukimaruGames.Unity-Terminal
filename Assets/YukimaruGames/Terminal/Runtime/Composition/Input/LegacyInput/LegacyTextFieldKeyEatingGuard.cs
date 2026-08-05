#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;

namespace YukimaruGames.Terminal.Composition.Input.LegacyInput
{
    /// <summary>
    /// ウィンドウの表示区間だけ<see cref="LegacyTextFieldKeyEatingScope"/>を有効化する
    /// <see cref="IWindowFocusInputGuard"/>実装.
    /// </summary>
    public sealed class LegacyTextFieldKeyEatingGuard : IWindowFocusInputGuard
    {
        /// <inheritdoc/>
        public IDisposable BeginScope() => new LegacyTextFieldKeyEatingScope();
    }
}
#endif

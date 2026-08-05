#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
using System;

namespace YukimaruGames.Terminal.Composition.Input.LegacyInput
{
    /// <summary>
    /// <see cref="UnityEngine.Input.eatKeyPressOnTextFieldFocus"/>を無効化するスコープ.
    /// IMGUIのTextFieldがフォーカス(キャレット)を保持している間、既定(true)では
    /// レガシーInput Managerがキー入力そのものを飲み込み、<see cref="UnityEngine.Input.GetKeyDown"/>等が
    /// 反応しなくなる(Return/Escape/Tab/矢印キー/修飾キーを含む)。ターミナルの入力欄は常にこの状態を
    /// 想定するため、生存期間中は無効化し、破棄時に元の値へ復元する.
    /// </summary>
    public sealed class LegacyTextFieldKeyEatingScope : IDisposable
    {
        private readonly bool _previous;
        private bool _disposed;

        public LegacyTextFieldKeyEatingScope()
        {
#pragma warning disable CS0618 // eatKeyPressOnTextFieldFocusはObsolete指定だが、この挙動を回避する唯一の公式手段.
            _previous = UnityEngine.Input.eatKeyPressOnTextFieldFocus;
            UnityEngine.Input.eatKeyPressOnTextFieldFocus = false;
#pragma warning restore CS0618
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

#pragma warning disable CS0618
            UnityEngine.Input.eatKeyPressOnTextFieldFocus = _previous;
#pragma warning restore CS0618
        }
    }
}
#endif

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
    /// 反応しなくなる(Return/Escape/Tab/矢印キー/修飾キーを含む)。生成時点で無効化し、
    /// <see cref="Dispose"/>で必ず元の値へ復元する.
    /// </summary>
    /// <remarks>
    /// <see cref="UnityEngine.Input.eatKeyPressOnTextFieldFocus"/>はプロセスグローバルな設定のため、
    /// このスコープが有効な間はホスト側(ターミナル外)のレガシーキーバインドも文字入力中に反応するように
    /// なる。呼び出し側はウィンドウが表示されている期間のみ生成し、閉じたら即座に破棄すること.
    /// 同一プロセス内で複数インスタンスが同時に生存する場合(複数ターミナルを同時に開く等)に備え、
    /// 生成・破棄をプロセス全体で参照カウントし、最初の1つが元の値を保存、最後の1つが復元する.
    /// </remarks>
    public sealed class LegacyTextFieldKeyEatingScope : IDisposable
    {
        private static int _activeScopeCount;
        private static bool _previous;

        private bool _disposed;

        public LegacyTextFieldKeyEatingScope()
        {
            if (_activeScopeCount == 0)
            {
#pragma warning disable CS0618 // eatKeyPressOnTextFieldFocusはObsolete指定だが、この挙動を回避する唯一の公式手段.
                _previous = UnityEngine.Input.eatKeyPressOnTextFieldFocus;
                UnityEngine.Input.eatKeyPressOnTextFieldFocus = false;
#pragma warning restore CS0618
            }

            ++_activeScopeCount;
        }

        // 外部からの不用意な直接呼び出しを避けるため、明示的インターフェース実装とする.
        // 呼び出し側は必ず IDisposable として扱うこと(using / IDisposable変数経由).
        void IDisposable.Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            --_activeScopeCount;
            if (_activeScopeCount > 0) return;

#pragma warning disable CS0618
            UnityEngine.Input.eatKeyPressOnTextFieldFocus = _previous;
#pragma warning restore CS0618
        }
    }
}
#endif

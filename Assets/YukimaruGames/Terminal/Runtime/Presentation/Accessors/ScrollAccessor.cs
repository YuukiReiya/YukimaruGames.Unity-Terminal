using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;

namespace YukimaruGames.Terminal.Presentation.Accessors
{
    public sealed class ScrollAccessor : IScrollAccessor
    {
        private Vector2 _scrollPosition;

        public Vector2 ScrollPosition
        {
            get => _scrollPosition;
            set
            {
                if (_scrollPosition == value) return;

                _scrollPosition = value;
                OnScrollChanged?.Invoke(value);
            }
        }

        public event Action<Vector2> OnScrollChanged;

        /// <summary>
        /// <see cref="ScrollPosition"/>のセッターと異なり<see cref="OnScrollChanged"/>を発火せず、
        /// 保持値のみを実際の描画結果と同期する.
        /// </summary>
        /// <remarks>
        /// UIToolkit版では毎フレーム実際のScrollView.scrollOffsetを書き戻すことで
        /// <see cref="ScrollToEnd"/>のセンチネル(float.MaxValue)をリセットしているが、通常の
        /// セッター経由だと浮動小数の揺れでほぼ毎フレーム値が変化したと判定され、その都度
        /// <see cref="OnScrollChanged"/>経由でScrollView側への書き戻しが再度走る不要な相互フィード
        /// バックが発生し、体感できるレベルの入力ラグを引き起こしていた(#122、実機検証で確認)。
        /// 通知を伴わないこのメソッドで同期することでこれを避ける.
        /// </remarks>
        public void SyncPosition(Vector2 position) => _scrollPosition = position;

        public void ScrollToEnd()
        {
            if (Mathf.Approximately(_scrollPosition.y, float.MaxValue)) return;
            _scrollPosition.y = float.MaxValue;
            OnScrollChanged?.Invoke(_scrollPosition);
        }
    }
}

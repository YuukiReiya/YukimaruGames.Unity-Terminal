using System;
using System.Collections.Generic;
using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Models.Event
{
    /// <summary>
    /// 同一フレームで複数の<see cref="TerminalAction"/>が同時に成立した場合に、
    /// どれか1つだけを選ぶための優先順位. <see cref="TerminalAction"/>のみに依存し、キー種別には
    /// 依存しないため、将来の非キー依存バックエンドでもそのまま再利用できる.
    /// </summary>
    [Serializable]
    public sealed class TerminalActionPriority
    {
        // 数値が小さいほど優先度が高い。既定値は全アクションで一意(同値なし)。
        [SerializeField] private int _cancel = 0; // 最優先: 実行中コマンドの中断
        [SerializeField] private int _close = 1; // negative(閉じる)をpositive(開く)より優先
        [SerializeField] private int _open = 2;
        [SerializeField] private int _execute = 3;
        [SerializeField] private int _previousHistory = 4;
        [SerializeField] private int _nextHistory = 5;
        [SerializeField] private int _autocomplete = 6;
        [SerializeField] private int _focus = 7;

        private int GetOrder(TerminalAction action) => action switch
        {
            TerminalAction.Cancel => _cancel,
            TerminalAction.Close => _close,
            TerminalAction.Open => _open,
            TerminalAction.Execute => _execute,
            TerminalAction.PreviousHistory => _previousHistory,
            TerminalAction.NextHistory => _nextHistory,
            TerminalAction.Autocomplete => _autocomplete,
            TerminalAction.Focus => _focus,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

        /// <summary>
        /// satisfiedActions(このフレームで条件を満たしている全アクション)の中で、
        /// actionが最も優先度が高い1つであるかどうかを判定する.
        /// </summary>
        /// <remarks>
        /// <see cref="TerminalAction.None"/>を渡してはならない. 優先度がInspectorから編集可能なため、
        /// ユーザーが誤って同じ数値を複数アクションに設定してしまう可能性がある。その場合でも
        /// enum宣言順を第2キーにしたタイブレークにより、常に一意な勝者が決まるようにしている.
        /// </remarks>
        public bool IsHighestPriority(TerminalAction action, IReadOnlyList<TerminalAction> satisfiedActions)
        {
            var myOrder = GetOrder(action);
            for (var i = 0; i < satisfiedActions.Count; ++i)
            {
                var other = satisfiedActions[i];
                if (other == action) continue;

                var otherOrder = GetOrder(other);
                if (otherOrder < myOrder || (otherOrder == myOrder && other < action)) return false;
            }
            return true;
        }
    }
}

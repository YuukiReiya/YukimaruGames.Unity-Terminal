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
    /// <remarks>
    /// 配列のインデックスが優先度(小さいほど優先度が高い)を表す. Inspector上ではReorderableListとして
    /// ドラッグ&ドロップで並び替えることを想定しており、要素の追加・削除は行わず常に全8アクションを
    /// 保持する(<see cref="TerminalAction.None"/>を除く).
    /// </remarks>
    [Serializable]
    public sealed class TerminalActionPriority
    {
        [SerializeField]
        private TerminalAction[] _order =
        {
            TerminalAction.Cancel, // 最優先: 実行中コマンドの中断
            TerminalAction.Close, // negative(閉じる)をpositive(開く)より優先
            TerminalAction.Open,
            TerminalAction.Execute,
            TerminalAction.PreviousHistory,
            TerminalAction.NextHistory,
            TerminalAction.Autocomplete,
            TerminalAction.Focus,
        };

        private int GetOrder(TerminalAction action)
        {
            for (var i = 0; i < _order.Length; ++i)
            {
                if (_order[i] == action) return i;
            }
            throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        /// <summary>
        /// satisfiedActions(このフレームで条件を満たしている全アクション)の中で、
        /// actionが最も優先度が高い1つであるかどうかを判定する.
        /// </summary>
        /// <remarks><see cref="TerminalAction.None"/>を渡してはならない.</remarks>
        public bool IsHighestPriority(TerminalAction action, IReadOnlyList<TerminalAction> satisfiedActions)
        {
            var myOrder = GetOrder(action);
            for (var i = 0; i < satisfiedActions.Count; ++i)
            {
                var other = satisfiedActions[i];
                if (other == action) continue;
                if (GetOrder(other) < myOrder) return false;
            }
            return true;
        }
    }
}

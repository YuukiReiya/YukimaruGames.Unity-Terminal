using System.Collections.Generic;

namespace YukimaruGames.Terminal.Presentation.Models.Event
{
    /// <summary>
    /// 同一フレームで複数の<see cref="TerminalAction"/>が同時に成立した場合に、
    /// どれか1つだけを選ぶための優先順位.
    /// </summary>
    public static class TerminalActionPriority
    {
        // 数値が小さいほど優先度が高い。全アクションで一意(同値なし)。
        private static readonly Dictionary<TerminalAction, int> Order = new()
        {
            { TerminalAction.Cancel, 0 },   // 最優先: 実行中コマンドの中断
            { TerminalAction.Close, 1 },    // negative(閉じる)をpositive(開く)より優先
            { TerminalAction.Open, 2 },
            { TerminalAction.Execute, 3 },
            { TerminalAction.PreviousHistory, 4 },
            { TerminalAction.NextHistory, 5 },
            { TerminalAction.Autocomplete, 6 },
            { TerminalAction.Focus, 7 },
        };

        /// <summary>
        /// satisfiedActions(このフレームで条件を満たしている全アクション)の中で、
        /// actionが最も優先度が高い1つであるかどうかを判定する.
        /// </summary>
        /// <remarks><see cref="TerminalAction.None"/>を渡してはならない.</remarks>
        public static bool IsHighestPriority(TerminalAction action, IReadOnlyList<TerminalAction> satisfiedActions)
        {
            var myOrder = Order[action];
            for (var i = 0; i < satisfiedActions.Count; ++i)
            {
                var other = satisfiedActions[i];
                if (other == action) continue;
                if (Order[other] < myOrder) return false;
            }
            return true;
        }
    }
}

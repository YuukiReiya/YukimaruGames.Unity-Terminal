using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Presentation.Models.Event
{
    /// <summary>
    /// 各<see cref="TerminalAction"/>を「押下(Pressed)」「離上(Released)」のどちらのタイミングで
    /// 発火とみなすかを保持する. <see cref="TerminalAction"/>のみに依存し、キー種別
    /// (InputSystemの<c>Key</c>やLegacyの<c>KeyCode</c>等)には一切依存しないため、
    /// 将来の非キー依存バックエンドでもそのまま再利用できる.
    /// </summary>
    [Serializable]
    public sealed class TerminalActionTriggerTiming
    {
        public enum Timing
        {
            Pressed,
            Released,
        }

        [SerializeField] private Timing _open = Timing.Released;
        [SerializeField] private Timing _close = Timing.Released;
        [SerializeField] private Timing _execute = Timing.Pressed;
        [SerializeField] private Timing _cancel = Timing.Pressed;
        [SerializeField] private Timing _previousHistory = Timing.Pressed;
        [SerializeField] private Timing _nextHistory = Timing.Pressed;
        [SerializeField] private Timing _autocomplete = Timing.Pressed;
        [SerializeField] private Timing _focus = Timing.Pressed;

        /// <summary>指定アクションの発火タイミングを取得する.</summary>
        public Timing GetTiming(TerminalAction action) => action switch
        {
            TerminalAction.Open => _open,
            TerminalAction.Close => _close,
            TerminalAction.Execute => _execute,
            TerminalAction.Cancel => _cancel,
            TerminalAction.PreviousHistory => _previousHistory,
            TerminalAction.NextHistory => _nextHistory,
            TerminalAction.Autocomplete => _autocomplete,
            TerminalAction.Focus => _focus,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
    }
}

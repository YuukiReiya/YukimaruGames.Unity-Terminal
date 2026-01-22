using System;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <inheritdoc cref="ITerminalButtonTheme" />
    /// <remarks>
    /// UnityのInspectorで設定可能なボタン配色実装です。
    /// </remarks>
    [Serializable]
    public sealed class TerminalButtonTheme : ITerminalButtonTheme
    {
        [SerializeField] private Color _execute = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _copy = new(0f, 0.7f, 0.8f);
        [SerializeField] private Color _base = new(0f, 0.7f, 0.8f);

        /// <inheritdoc />
        public TerminalColor Execute => ToTerminalColor(_execute);

        /// <inheritdoc />
        public TerminalColor Copy => ToTerminalColor(_copy);

        /// <inheritdoc />
        public TerminalColor Base => ToTerminalColor(_base);

        private static TerminalColor ToTerminalColor(Color color) => new(color.r, color.g, color.b, color.a);
    }
}

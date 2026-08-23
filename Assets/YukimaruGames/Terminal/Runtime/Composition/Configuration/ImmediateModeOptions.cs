using System;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// Immediate Mode(IMGUI)ベースの標準実装における<see cref="ITerminalOptions"/>実装.
    /// </summary>
    [Serializable, AddTypeMenu("IMGUI Options")]
    public sealed class ImmediateModeOptions : ITerminalOptions
    {
        [Header("Input Settings")]
        [SerializeReference, SerializeInterface]
        private ITerminalInput _input = new ImmediateModeInput();

        [Header("Command Settings")]
        [SerializeField] private int _bufferSize = 256;
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;

        [Header("UI Controls")]
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;
        [SerializeField] private bool _showLoadingIndicator = true;
        [SerializeField] private string[] _loadingIndicatorFrames = { "|", "/", "-", "\\" };

        /// <inheritdoc/>
        public ITerminalInput Input => _input ?? new NullInput();

        /// <inheritdoc/>
        public int BufferSize => _bufferSize;
        /// <inheritdoc/>
        public string Prompt => _prompt;
        /// <inheritdoc/>
        public string BootupCommand => _bootupCommand;
        /// <inheritdoc/>
        public bool IsButtonVisible => _buttonVisible;
        /// <inheritdoc/>
        public bool IsButtonReverse => _buttonReverse;
        /// <inheritdoc/>
        public bool ShowLoadingIndicator => _showLoadingIndicator;
        /// <inheritdoc/>
        public string[] LoadingIndicatorFrames => _loadingIndicatorFrames;
    }
}

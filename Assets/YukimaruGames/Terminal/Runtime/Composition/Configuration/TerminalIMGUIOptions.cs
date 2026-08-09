using System;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// IMGUIベースの標準実装における<see cref="ITerminalOptions"/>実装.
    /// </summary>
    [Serializable]
    public sealed class TerminalIMGUIOptions : ITerminalOptions
    {
        [Header("Input Settings")]
        [SerializeReference, SerializeInterface]
        private ITerminalInput _input = new TerminalIMGUIInput();

        [Header("Command Settings")]
        [SerializeField] private int _bufferSize = 256;
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;

        [Header("UI Controls")]
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;
        [SerializeField] private bool _showLoadingIndicator = true;
        [SerializeField] private string[] _loadingIndicatorFrames = { "|", "/", "-", "\\" };

        [Header("Mode Settings")]
        [Tooltip("コマンド走査に追加するアセンブリ名. 独自asmdefにコマンドやモードを置く場合はここに列挙する.")]
        [SerializeField] private string[] _additionalCommandAssemblies = Array.Empty<string>();

        /// <inheritdoc/>
        public ITerminalInput Input => _input ?? new TerminalNullInput();

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
        /// <inheritdoc/>
        public string[] AdditionalCommandAssemblies => _additionalCommandAssemblies;
    }
}

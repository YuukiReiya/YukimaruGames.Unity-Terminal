using System;
using UnityEngine;
using YukimaruGames.Terminal.SharedKernel;
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
using YukimaruGames.Terminal.Runtime.Input.LegacyInput;

namespace YukimaruGames.Terminal.Domain.Settings
{
    /// <inheritdoc cref="ITerminalOptions" />
    /// <remarks>
    /// UnityのInspectorで設定可能なターミナルの機能設定実装です。
    /// </remarks>
    [Serializable]
    public sealed class TerminalOptions : ITerminalOptions
    {
        [SerializeField] private InputKeyboardType _inputKeyboardType = InputKeyboardType.InputSystem;

#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private LegacyInputKey _legacyInputKey;
#endif

#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputSystemKey _inputSystemKey;
#endif

        [SerializeField] private TerminalState _bootupWindowState = TerminalState.Close;
        [SerializeField] private TerminalAnchor _anchor = TerminalAnchor.Top;
        [SerializeField] private TerminalWindowStyle _windowStyle = TerminalWindowStyle.Compact;
        [SerializeField, Min(1)] private int _bufferSize = 256;
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;

        /// <inheritdoc />
        public InputKeyboardType InputKeyboardType => _inputKeyboardType;

#if ENABLE_LEGACY_INPUT_MANAGER
        /// <summary>
        /// Legacy Input Manager使用時の入力キー設定を取得します。
        /// </summary>
        /// <remarks>
        /// インターフェースには定義されていませんが、具象クラスでアセットを保持します。
        /// </remarks>
        public LegacyInputKey LegacyInputKey => _legacyInputKey;
#endif

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Input System使用時の入力キー設定を取得します。
        /// </summary>
        /// <remarks>
        /// インターフェースには定義されていませんが、具象クラスでアセットを保持します。
        /// </remarks>
        public InputSystemKey InputSystemKey => _inputSystemKey;
#endif

        /// <inheritdoc />
        public TerminalState BootupWindowState => _bootupWindowState;

        /// <inheritdoc />
        public TerminalAnchor Anchor => _anchor;

        /// <inheritdoc />
        public TerminalWindowStyle WindowStyle => _windowStyle;

        /// <inheritdoc />
        public int BufferSize => _bufferSize;

        /// <inheritdoc />
        public string Prompt => _prompt;

        /// <inheritdoc />
        public string BootupCommand => _bootupCommand;

        /// <inheritdoc />
        public bool ButtonVisible => _buttonVisible;

        /// <inheritdoc />
        public bool ButtonReverse => _buttonReverse;
    }
}

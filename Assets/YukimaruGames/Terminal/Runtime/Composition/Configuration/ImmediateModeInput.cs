#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Composition.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
#endif

using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// Immediate Mode(IMGUI)ベースの標準実装における<see cref="ITerminalInput"/>実装.
    /// </summary>
    [Serializable]
    public sealed class ImmediateModeInput : ITerminalInput
    {
        [SerializeField] private InputKeyboardType _inputKeyboardType = InputKeyboardType.InputSystem;

#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private bool _allowKeyInputWhileTextFieldFocused = true;
        [SerializeField] private LegacyInputKey _legacyInputKey = new();
#endif

#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputSystemKey _inputSystemKey = new();
#endif

        [SerializeField] private TerminalActionTriggerTiming _triggerTiming = new();
        [SerializeField] private TerminalActionPriority _priority = new();

        /// <inheritdoc/>
        public InputKeyboardType InputKeyboardType => _inputKeyboardType;

#if ENABLE_LEGACY_INPUT_MANAGER
        /// <inheritdoc/>
        public bool AllowKeyInputWhileTextFieldFocused => _allowKeyInputWhileTextFieldFocused;
#else
        /// <inheritdoc/>
        public bool AllowKeyInputWhileTextFieldFocused => true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        /// <inheritdoc/>
        public LegacyInputKey LegacyInputKey => _legacyInputKey;
#endif

#if ENABLE_INPUT_SYSTEM
        /// <inheritdoc/>
        public InputSystemKey InputSystemKey => _inputSystemKey;
#endif

        /// <inheritdoc/>
        public TerminalActionTriggerTiming TriggerTiming => _triggerTiming;
        /// <inheritdoc/>
        public TerminalActionPriority Priority => _priority;
    }
}

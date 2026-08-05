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
    [Serializable]
    public sealed class TerminalStandardInput : ITerminalInput
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

        public InputKeyboardType InputKeyboardType => _inputKeyboardType;

#if ENABLE_LEGACY_INPUT_MANAGER
        public bool AllowKeyInputWhileTextFieldFocused => _allowKeyInputWhileTextFieldFocused;
#else
        public bool AllowKeyInputWhileTextFieldFocused => true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        public LegacyInputKey LegacyInputKey => _legacyInputKey;
#endif

#if ENABLE_INPUT_SYSTEM
        public InputSystemKey InputSystemKey => _inputSystemKey;
#endif

        public TerminalActionTriggerTiming TriggerTiming => _triggerTiming;
        public TerminalActionPriority Priority => _priority;
    }
}

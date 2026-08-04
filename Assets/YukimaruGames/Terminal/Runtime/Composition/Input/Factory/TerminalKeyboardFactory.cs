//#undef ENABLE_INPUT_SYSTEM
//#undef ENABLE_LEGACY_INPUT_MANAGER
using System;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Models.Event;

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Composition.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
#endif

namespace YukimaruGames.Terminal.Composition
{
    public sealed class TerminalKeyboardFactory
    {
#if ENABLE_INPUT_SYSTEM
        private readonly InputSystemKey _inputSystemKey;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private readonly LegacyInputKey _legacyInputKey;
#endif

        private readonly TerminalActionTriggerTiming _triggerTiming;
        private readonly TerminalActionPriority _priority;

#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        public TerminalKeyboardFactory(
            InputSystemKey inputSystemKey,
            LegacyInputKey legacyInputKey,
            TerminalActionTriggerTiming triggerTiming,
            TerminalActionPriority priority)
        {
            _inputSystemKey = inputSystemKey;
            _legacyInputKey = legacyInputKey;
            _triggerTiming = triggerTiming;
            _priority = priority;
        }
#elif ENABLE_INPUT_SYSTEM
        public TerminalKeyboardFactory(
            InputSystemKey inputSystemKey,
            TerminalActionTriggerTiming triggerTiming,
            TerminalActionPriority priority)
        {
            _inputSystemKey = inputSystemKey;
            _triggerTiming = triggerTiming;
            _priority = priority;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        public TerminalKeyboardFactory(
            LegacyInputKey legacyInputKey,
            TerminalActionTriggerTiming triggerTiming,
            TerminalActionPriority priority)
        {
            _legacyInputKey = legacyInputKey;
            _triggerTiming = triggerTiming;
            _priority = priority;
        }
#else // 両未定義であれば空コンストラクタのみのサポートなので暗黙的なコンストラクタで事足りる.
#endif

        public IKeyboardInputHandler Create(InputKeyboardType keyboardType) =>
            keyboardType switch
            {
                InputKeyboardType.None => null,
                InputKeyboardType.InputSystem =>
#if ENABLE_INPUT_SYSTEM
                    new InputSystemKeyboardHandler(_inputSystemKey, _triggerTiming, _priority)
#else
                    throw new NotSupportedException($"InputKeyboardType.InputSystem is selected, but the 'Input System' package is not installed.{Environment.NewLine}Please install the package via the Package Manager or switch to 'Legacy'.")
#endif
                ,
                InputKeyboardType.Legacy =>
#if ENABLE_LEGACY_INPUT_MANAGER
                    new LegacyInputKeyboardHandler(_legacyInputKey, _triggerTiming, _priority)
#else
                    throw new NotSupportedException($"InputKeyboardType.Legacy is selected, but 'Enable Legacy Input Manager' is not active in Project Settings.")
#endif
                ,
                _ => throw new ArgumentOutOfRangeException(nameof(keyboardType), keyboardType, null)
            };
    }
}

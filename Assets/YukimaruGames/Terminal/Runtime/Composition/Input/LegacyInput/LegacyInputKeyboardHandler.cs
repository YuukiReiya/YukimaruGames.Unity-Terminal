#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Composition.Input.LegacyInput
{
    public sealed class LegacyInputKeyboardHandler : IKeyboardInputHandler
    {
        private readonly LegacyInputKey _legacyInputKey;

        public LegacyInputKeyboardHandler(LegacyInputKey legacyInputKey)
        {
            _legacyInputKey = legacyInputKey;
        }

        public bool WasPressedThisFrame(TerminalAction action)
        {
            var keyCode = _legacyInputKey.GetKey(action);
            return keyCode is not UnityEngine.KeyCode.None && IsModifierSatisfied(action) && UnityEngine.Input.GetKeyDown(keyCode);
        }

        public bool WasReleasedThisFrame(TerminalAction action)
        {
            var keyCode = _legacyInputKey.GetKey(action);
            return keyCode is not UnityEngine.KeyCode.None && IsModifierSatisfied(action) && UnityEngine.Input.GetKeyUp(keyCode);
        }

        private bool IsModifierSatisfied(TerminalAction action)
        {
            var modifier = _legacyInputKey.GetModifier(action);
            return modifier is UnityEngine.KeyCode.None || UnityEngine.Input.GetKey(modifier);
        }
    }
}
#endif

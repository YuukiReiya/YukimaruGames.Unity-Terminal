#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
using System.Collections.Generic;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Composition.Input.LegacyInput
{
    public sealed class LegacyInputKeyboardHandler : IKeyboardInputHandler
    {
        private static readonly TerminalAction[] AllActions =
        {
            TerminalAction.Open, TerminalAction.Close, TerminalAction.Execute, TerminalAction.Cancel,
            TerminalAction.PreviousHistory, TerminalAction.NextHistory, TerminalAction.Autocomplete, TerminalAction.Focus
        };

        private readonly LegacyInputKey _legacyInputKey;
        private readonly List<TerminalAction> _satisfiedBuffer = new(AllActions.Length);

        public LegacyInputKeyboardHandler(LegacyInputKey legacyInputKey)
        {
            _legacyInputKey = legacyInputKey;
        }

        public bool WasPressedThisFrame(TerminalAction action) => IsTriggered(action, isPressed: true);

        public bool WasReleasedThisFrame(TerminalAction action) => IsTriggered(action, isPressed: false);

        private bool IsTriggered(TerminalAction action, bool isPressed)
        {
            if (!IsBaseSatisfied(action, isPressed)) return false;

            // 同フレームで他に成立しているアクションを集め、優先度が最も高い場合のみ発火する.
            _satisfiedBuffer.Clear();
            for (var i = 0; i < AllActions.Length; ++i)
            {
                var candidate = AllActions[i];
                if (IsBaseSatisfied(candidate, isPressed)) _satisfiedBuffer.Add(candidate);
            }

            return TerminalActionPriority.IsHighestPriority(action, _satisfiedBuffer);
        }

        private bool IsBaseSatisfied(TerminalAction action, bool isPressed)
        {
            var keyCode = _legacyInputKey.GetKey(action);
            if (keyCode is UnityEngine.KeyCode.None) return false;
            if (!AreModifiersHeld(action)) return false;

            return isPressed ? UnityEngine.Input.GetKeyDown(keyCode) : UnityEngine.Input.GetKeyUp(keyCode);
        }

        private bool AreModifiersHeld(TerminalAction action)
        {
            var modifiers = _legacyInputKey.GetModifiers(action);
            for (var i = 0; i < modifiers.Count; ++i)
            {
                if (!UnityEngine.Input.GetKey(modifiers[i])) return false;
            }
            return true;
        }
    }
}
#endif

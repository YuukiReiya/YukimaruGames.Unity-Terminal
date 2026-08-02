using System.Collections.Generic;
using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Composition.Input.InputSystem
{
    /// <summary>
    /// InputSystemを用いて<see cref="TerminalAction"/>の入力判定を行う<see cref="IKeyboardInputHandler"/>実装.
    /// 同フレームで複数アクションが成立した場合は<see cref="TerminalActionPriority"/>に基づき最高優先度の
    /// アクションのみを発火する.
    /// </summary>
    public sealed class InputSystemKeyboardHandler : IKeyboardInputHandler
    {
        private static readonly TerminalAction[] AllActions =
        {
            TerminalAction.Open, TerminalAction.Close, TerminalAction.Execute, TerminalAction.Cancel,
            TerminalAction.PreviousHistory, TerminalAction.NextHistory, TerminalAction.Autocomplete, TerminalAction.Focus
        };

        private readonly InputSystemKey _inputSystemKey;
        private readonly List<TerminalAction> _satisfiedBuffer = new(AllActions.Length);

        public InputSystemKeyboardHandler(InputSystemKey inputSystemKey)
        {
            _inputSystemKey = inputSystemKey;
        }

        /// <inheritdoc/>
        public bool WasPressedThisFrame(TerminalAction action) => IsTriggered(action, isPressed: true);

        /// <inheritdoc/>
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
            var key = _inputSystemKey.GetKey(action);
            if (key is Key.None) return false;
            if (!AreModifiersHeld(action)) return false;

            var control = Keyboard.current?[key];
            if (control == null) return false;

            return isPressed ? control.wasPressedThisFrame : control.wasReleasedThisFrame;
        }

        private bool AreModifiersHeld(TerminalAction action)
        {
            var modifiers = _inputSystemKey.GetModifiers(action);
            var keyboard = Keyboard.current;
            if (keyboard == null) return modifiers.Count == 0;

            for (var i = 0; i < modifiers.Count; ++i)
            {
                var modifier = modifiers[i];
                if (modifier is Key.None) continue;
                if (!keyboard[modifier].isPressed) return false;
            }
            return true;
        }
    }
}

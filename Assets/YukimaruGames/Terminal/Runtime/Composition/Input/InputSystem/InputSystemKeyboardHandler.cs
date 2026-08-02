using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Models.Event;
using YukimaruGames.Terminal.Composition.Input.InputSystem;

namespace YukimaruGames.Terminal.Composition
{
    public sealed class InputSystemKeyboardHandler : IKeyboardInputHandler
    {
        private readonly InputSystemKey _inputSystemKey;

        public InputSystemKeyboardHandler(InputSystemKey inputSystemKey)
        {
            _inputSystemKey = inputSystemKey;
        }

        public bool WasPressedThisFrame(TerminalAction action)
        {
            var key = _inputSystemKey.GetKey(action);
            return key is not Key.None && IsModifierSatisfied(action) && (Keyboard.current?[key].wasPressedThisFrame ?? false);
        }

        public bool WasReleasedThisFrame(TerminalAction action)
        {
            var key = _inputSystemKey.GetKey(action);
            return key is not Key.None && IsModifierSatisfied(action) && (Keyboard.current?[key].wasReleasedThisFrame ?? false);
        }

        private bool IsModifierSatisfied(TerminalAction action)
        {
            var modifier = _inputSystemKey.GetModifier(action);
            return modifier is Key.None || (Keyboard.current?[modifier].isPressed ?? false);
        }
    }
}

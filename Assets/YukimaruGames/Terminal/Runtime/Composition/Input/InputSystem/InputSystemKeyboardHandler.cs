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
            return key is not Key.None && (Keyboard.current?[key].wasPressedThisFrame ?? false);
        }

        public bool WasReleasedThisFrame(TerminalAction action)
        {
            var key = _inputSystemKey.GetKey(action);
            return key is not Key.None && (Keyboard.current?[key].wasReleasedThisFrame ?? false);
        }
    }
}

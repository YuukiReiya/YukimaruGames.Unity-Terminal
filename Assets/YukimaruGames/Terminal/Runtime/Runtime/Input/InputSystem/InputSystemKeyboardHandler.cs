using UnityEngine.InputSystem;
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.UI.View.Model;

namespace YukimaruGames.Terminal.Runtime
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

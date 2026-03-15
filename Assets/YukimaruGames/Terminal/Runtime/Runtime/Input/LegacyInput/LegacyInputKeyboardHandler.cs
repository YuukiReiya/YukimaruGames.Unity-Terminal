#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Runtime.Shared;
using YukimaruGames.Terminal.UI;
using YukimaruGames.Terminal.UI.Core;
using YukimaruGames.Terminal.UI.Input;

namespace YukimaruGames.Terminal.Runtime.Input.LegacyInput
{
    public sealed class LegacyInputKeyboardHandler : IKeyboardInputHandler
    {
        private readonly LegacyInputKey _legacyInputKey;

        public LegacyInputKeyboardHandler(LegacyInputKey legacyInputKey)
        {
            _legacyInputKey = legacyInputKey;
        }

        public bool WasPressedThisFrame(Trigger action)
        {
            var keyCode = _legacyInputKey.GetKey(action);
            return keyCode is not UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyDown(keyCode);
        }

        public bool WasReleasedThisFrame(Trigger action)
        {
            var keyCode = _legacyInputKey.GetKey(action);
            return keyCode is not UnityEngine.KeyCode.None && UnityEngine.Input.GetKeyUp(keyCode);
        }
    }
}
#endif

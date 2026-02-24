#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Runtime.Input.LegacyInput;
#endif

namespace YukimaruGames.Terminal.Runtime
{
    public interface ITerminalOptions
    {
        InputKeyboardType InputKeyboardType { get; }
        
#if ENABLE_LEGACY_INPUT_MANAGER
        LegacyInputKey LegacyInputKey { get; }
#endif
        
#if ENABLE_INPUT_SYSTEM
        InputSystemKey InputSystemKey { get; }
#endif

        int BufferSize { get; }
        string Prompt { get; }
        string BootupCommand { get; }
        bool IsButtonVisible { get; }
        bool IsButtonReverse { get; }
    }
}

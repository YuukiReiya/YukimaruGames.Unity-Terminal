#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Composition.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
#endif

using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// キーボード入力に関する設定をまとめた薄いコンテナ. <see cref="ITerminalOptions"/>から参照される.
    /// </summary>
    public interface ITerminalInput
    {
        InputKeyboardType InputKeyboardType { get; }

#if ENABLE_LEGACY_INPUT_MANAGER
        LegacyInputKey LegacyInputKey { get; }
#endif

#if ENABLE_INPUT_SYSTEM
        InputSystemKey InputSystemKey { get; }
#endif

        TerminalActionTriggerTiming TriggerTiming { get; }
        TerminalActionPriority Priority { get; }
    }
}

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

        /// <summary>
        /// Legacy Input Manager選択時、ウィンドウ表示中に
        /// <see cref="UnityEngine.Input.eatKeyPressOnTextFieldFocus"/>を無効化し、
        /// 入力欄がフォーカスを持っていてもキー入力(Return/Escape等)を検知できるようにするか.
        /// true(既定)の場合、ウィンドウ表示中はホスト側のレガシーキーバインドにも影響する
        /// (プロセスグローバルな設定のため)点に注意.
        /// </summary>
        bool AllowKeyInputWhileTextFieldFocused { get; }

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

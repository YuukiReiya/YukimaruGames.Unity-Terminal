#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Composition.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Composition.Input.LegacyInput;
#endif

using System;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Presentation.Models.Event;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 最小限の設定値を持つ Null Object パターン実装.
    /// ユーザーが意図的に Input を null にした場合のフォールバック先.
    /// </summary>
    [Serializable, HideInTypeMenu]
    public sealed class TerminalNullInput : ITerminalInput
    {
        // 入力を無効化
        public InputKeyboardType InputKeyboardType => InputKeyboardType.None;
        // Legacy-onlyビルドではResolveKeyboardType()がInputKeyboardType(None)を無視して
        // 常にLegacyを返すため、falseにしてNull Input経由でLegacyTextFieldKeyEatingGuardが
        // 生成されない(=ホストのグローバル設定を変更しない)ようにする.
        /// <inheritdoc/>
        public bool AllowKeyInputWhileTextFieldFocused => false;

#if ENABLE_LEGACY_INPUT_MANAGER
        // デフォルトキー設定
        public LegacyInputKey LegacyInputKey => new LegacyInputKey();
#endif

#if ENABLE_INPUT_SYSTEM
        // デフォルトキー設定
        public InputSystemKey InputSystemKey => new InputSystemKey();
#endif

        // デフォルトのタイミング/優先度設定
        public TerminalActionTriggerTiming TriggerTiming => new TerminalActionTriggerTiming();
        public TerminalActionPriority Priority => new TerminalActionPriority();
    }
}

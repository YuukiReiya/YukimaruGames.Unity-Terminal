#if !UNITY_2019_2_OR_NEWER
#define ENABLE_LEGACY_INPUT_MANAGER
#endif

#if ENABLE_INPUT_SYSTEM
using YukimaruGames.Terminal.Runtime.Input.InputSystem;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
using YukimaruGames.Terminal.Runtime.Input.LegacyInput;
#endif

using System;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Runtime
{
        /// <summary>
        /// 最小限の設定値を持つ Null Object パターン実装.
        /// ユーザーが意図的に Options を null にした場合のフォールバック先.
        /// </summary>
        [Serializable, HideInTypeMenu]
        public sealed class TerminalNullOptions : ITerminalOptions
        {
                // 入力を無効化
                public InputKeyboardType InputKeyboardType => InputKeyboardType.None;

#if ENABLE_LEGACY_INPUT_MANAGER
                // デフォルトキー設定
                public LegacyInputKey LegacyInputKey => new LegacyInputKey();
#endif

#if ENABLE_INPUT_SYSTEM
                // デフォルトキー設定
                public InputSystemKey InputSystemKey => new InputSystemKey();
#endif

                // 最小限のバッファ
                public int BufferSize => 0;

                // シンプルなプロンプト
                public string Prompt => string.Empty;

                // 起動コマンドなし
                public string BootupCommand => string.Empty;

                // ボタン非表示
                public bool IsButtonVisible => false;

                // ボタン順序はデフォルト
                public bool IsButtonReverse => false;
        }
}
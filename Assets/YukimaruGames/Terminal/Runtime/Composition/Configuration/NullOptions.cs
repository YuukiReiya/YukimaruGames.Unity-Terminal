using System;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
        /// <summary>
        /// 最小限の設定値を持つ Null Object パターン実装.
        /// ユーザーが意図的に Options を null にした場合のフォールバック先.
        /// </summary>
        [Serializable, HideInTypeMenu]
        public sealed class NullOptions : ITerminalOptions
        {
                // 入力を無効化
                public ITerminalInput Input => new NullInput();

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

                /// <inheritdoc/>
                public bool ShowLoadingIndicator => false;

                /// <inheritdoc/>
                public string[] LoadingIndicatorFrames => System.Array.Empty<string>();
        }
}

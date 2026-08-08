using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="CommandLineInstaller"/>専用の設定.
    /// </summary>
    /// <remarks>
    /// <see cref="TerminalStandardOptions"/>はIMGUI描画(ボタン表示・ローディングインジケータ・
    /// キーボード入力方式等)を前提としたパラメータを多く含むが、外部ターミナル(cmd/zsh)では
    /// コマンド実行系(バッファサイズ・プロンプト・追加コマンドアセンブリ)以外は一切使われない
    /// ため、それらを持たない専用の設定型として分離している.
    /// </remarks>
    [Serializable]
    public sealed class CommandLineOptions : ITerminalOptions
    {
        private const int DefaultBufferSize = 256;
        private const string DefaultPrompt = "$";

        [Header("Command Settings")]
        [Min(0)]
        [SerializeField] private int _bufferSize = DefaultBufferSize;
        [SerializeField] private string _prompt = DefaultPrompt;

        [Header("Mode Settings")]
        [Tooltip("コマンド走査に追加するアセンブリ名. 独自asmdefにコマンドやモードを置く場合はここに列挙する.")]
        [SerializeField] private string[] _additionalCommandAssemblies = Array.Empty<string>();

        /// <summary>
        /// 外部ターミナルはOS側のキーボード入力をそのまま使うため、Unity側のキーボード入力方式は不問.
        /// </summary>
        public ITerminalInput Input => new TerminalNullInput();

        public int BufferSize => _bufferSize;
        public string Prompt => _prompt;

        /// <summary>外部ターミナルにはIMGUIの入力欄が無いため未使用.</summary>
        public string BootupCommand => string.Empty;
        /// <summary>外部ターミナルにはランチャーボタンが無いため未使用.</summary>
        public bool IsButtonVisible => false;
        /// <summary>外部ターミナルにはランチャーボタンが無いため未使用.</summary>
        public bool IsButtonReverse => false;
        /// <summary>外部ターミナルにはローディングインジケータの描画が無いため未使用.</summary>
        public bool ShowLoadingIndicator => false;
        /// <summary>外部ターミナルにはローディングインジケータの描画が無いため未使用.</summary>
        public string[] LoadingIndicatorFrames => Array.Empty<string>();

        /// <inheritdoc/>
        public string[] AdditionalCommandAssemblies => _additionalCommandAssemblies;
    }
}

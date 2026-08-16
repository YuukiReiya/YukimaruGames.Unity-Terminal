using System;
using UnityEngine;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// <see cref="CommandLineInstaller"/>専用の設定.
    /// </summary>
    /// <remarks>
    /// <see cref="ImmediateModeOptions"/>はIMGUI描画(ボタン表示・ローディングインジケータ・
    /// キーボード入力方式等)を前提としたパラメータを多く含むが、外部ターミナル(cmd/zsh)では
    /// コマンド実行系(バッファサイズ・プロンプト・追加コマンドアセンブリ)以外は一切使われない
    /// ため、それらを持たない専用の設定型として分離している.
    /// </remarks>
    [Serializable]
    public sealed class CommandLineOptions : ITerminalOptions
    {
        private const int DefaultBufferSize = 256;
        private const string DefaultPrompt = "$";
        private const string CommandSettingsHeader = "Command Settings";
        private const string ModeSettingsHeader = "Mode Settings";
        private const string AdditionalCommandAssembliesTooltip =
            "コマンド走査に追加するアセンブリ名. 独自asmdefにコマンドやモードを置く場合はここに列挙する.";

        [Header(CommandSettingsHeader)]
        [Min(0)]
        [SerializeField] private int _bufferSize = DefaultBufferSize;
        [SerializeField] private string _prompt = DefaultPrompt;

        [Header(ModeSettingsHeader)]
        [Tooltip(AdditionalCommandAssembliesTooltip)]
        [SerializeField] private string[] _additionalCommandAssemblies = Array.Empty<string>();

        [Tooltip("外部ターミナルを自動で起動する。切ると待ち受けのみ行い、接続用のコマンドラインをUnityのコンソールへ出力する（既に開いているターミナルから接続したい場合）。")]
        [SerializeField] private bool _launchExternalTerminal = true;

        /// <summary>
        /// 外部ターミナルはOS側のキーボード入力をそのまま使うため、Unity側のキーボード入力方式は不問.
        /// </summary>
        public ITerminalInput Input => new NullInput();

        /// <summary>
        /// 保持するコマンドログの最大数.
        /// </summary>
        public int BufferSize => _bufferSize;

        /// <summary>
        /// 外部ターミナルに表示するプロンプト文字列.
        /// </summary>
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

        /// <summary>外部ターミナルを自動起動するか.</summary>
        public bool LaunchExternalTerminal => _launchExternalTerminal;
    }
}

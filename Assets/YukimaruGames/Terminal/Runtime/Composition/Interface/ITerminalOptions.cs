namespace YukimaruGames.Terminal.Composition
{
    public interface ITerminalOptions
    {
        ITerminalInput Input { get; }

        int BufferSize { get; }
        string Prompt { get; }
        string BootupCommand { get; }
        bool IsButtonVisible { get; }
        bool IsButtonReverse { get; }

        /// <summary>
        /// コマンド実行中にローディング表現(スピナー)を表示するかどうか.
        /// </summary>
        bool ShowLoadingIndicator { get; }

        /// <summary>
        /// ローディング表現として順番に表示するフレーム文字列群.
        /// </summary>
        string[] LoadingIndicatorFrames { get; }

        /// <summary>
        /// コマンド走査に追加するアセンブリ名. 既定は空(=Assembly-CSharpとその参照のみ)。
        /// 独自asmdefにコマンドやモードを置く場合はここに列挙する.
        /// </summary>
        string[] AdditionalCommandAssemblies { get; }
    }
}

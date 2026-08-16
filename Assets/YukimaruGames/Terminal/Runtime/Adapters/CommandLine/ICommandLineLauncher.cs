using System.Diagnostics;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// OSネイティブの外部ターミナル(cmd.exe/Terminal.app等)を起動し、
    /// <see cref="CommandLineBridge"/>のループバックポートへ接続する中継プロセスを立ち上げる契約.
    /// </summary>
    public interface ICommandLineLauncher
    {
        /// <summary>
        /// 現在の実行環境でこのランチャーが利用可能か.
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// 外部ターミナルを起動し、指定ポートへ接続する中継スクリプトを実行する.
        /// </summary>
        /// <param name="port">接続先の127.0.0.1ループバックポート</param>
        /// <param name="token">
        /// 接続直後に中継スクリプトが送る認証用トークン(<see cref="CommandLineBridge.Token"/>).
        /// 実装は、他プロセスから覗かれないようこれを起動引数として直接渡さず、
        /// 一時ファイル経由で中継スクリプトへ受け渡すこと.
        /// </param>
        /// <returns>起動したプロセス(起動できなかった場合はnull)</returns>
        Process Launch(int port, string token);

        /// <summary>
        /// 中継スクリプトとトークンファイルを書き出し、利用者が手で実行できるコマンドラインを返す.
        /// </summary>
        /// <remarks>
        /// 既に開いている別のターミナルから接続したい場合に使う(#160)。
        /// <see cref="Launch"/>が内部で行っている準備のうち、外部ターミナルの起動だけを行わない。
        /// <para>
        /// 戻り値にトークンそのものは含めない。トークンを書いた一時ファイルのパスを示すに留める
        /// (画面やログに出る文字列であり、そこへ認証情報を載せない).
        /// </para>
        /// </remarks>
        /// <returns>
        /// そのまま貼り付けて実行できるコマンドライン。準備できなかった場合は<c>null</c>.
        /// </returns>
        string BuildConnectionCommand(int port, string token);
    }
}

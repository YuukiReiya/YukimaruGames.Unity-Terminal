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
    }
}

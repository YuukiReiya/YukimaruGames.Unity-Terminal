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
        /// <returns>起動したプロセス(起動できなかった場合はnull)</returns>
        Process Launch(int port);
    }
}

using System.Diagnostics;

namespace YukimaruGames.Terminal.Adapters.CliTerminal
{
    /// <summary>
    /// OSネイティブの外部ターミナル(cmd.exe/Terminal.app等)を起動し、
    /// <see cref="CliTerminalBridge"/>のループバックポートへ接続する中継プロセスを立ち上げる契約.
    /// </summary>
    public interface ICliTerminalLauncher
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

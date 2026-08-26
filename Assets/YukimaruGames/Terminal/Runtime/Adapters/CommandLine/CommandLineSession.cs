using System;
using System.Diagnostics;
using YukimaruGames.Terminal.Application.Interfaces;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// <see cref="CommandLineBridge"/>(TCP中継)と<see cref="ICommandLineLauncher"/>
    /// (外部プロセス起動)を束ね、外部ターミナルの開始/終了ライフサイクルを管理するオーケストレータ.
    /// </summary>
    public sealed class CommandLineSession : IDisposable
    {
        private readonly ITerminalService _service;
        private readonly ICommandLineLauncher _launcher;
        private readonly bool _launchExternalTerminal;

        private CommandLineBridge _bridge;
        private Process _process;
        private bool _disposed;

        /// <summary>
        /// 外部ターミナルが接続待ち/接続中か.
        /// </summary>
        public bool IsOpen => _bridge != null;

        /// <summary>
        /// 利用者が手で実行して接続するためのコマンドライン(準備できていない場合は<c>null</c>).
        /// </summary>
        /// <remarks>
        /// 自動起動しない設定のとき、接続前のクライアントにはログが届かず過去ログも送られないため、
        /// これを得る手段が他に無い。このアセンブリはUnity非依存(<c>noEngineReferences</c>)で
        /// クリップボードにもコンソールにも触れないため、利用者への受け渡しはComposition層に委ねる(#160).
        /// </remarks>
        public string ConnectionCommand { get; private set; }

        /// <param name="launchExternalTerminal">
        /// 外部ターミナルを自動起動するか。<c>false</c>の場合は中継の待ち受けだけを行い、
        /// 利用者が既に開いているターミナルから接続できるよう、接続用のコマンドラインを案内する(#160).
        /// </param>
        public CommandLineSession(
            ITerminalService service,
            ICommandLineLauncher launcher = null,
            bool launchExternalTerminal = true)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _launcher = launcher ?? CreatePlatformLauncher();
            _launchExternalTerminal = launchExternalTerminal;
        }

        private static ICommandLineLauncher CreatePlatformLauncher()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return new WindowsCommandLineLauncher();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return new MacCommandLineLauncher();
#else
            return new NullCommandLineLauncher();
#endif
        }

        /// <summary>
        /// 外部ターミナルを起動し、コマンド入出力の中継を開始する.
        /// 既に開いている場合は何もしない.
        /// </summary>
        public void Open()
        {
            if (_disposed || IsOpen)
            {
                return;
            }

            if (!_launcher.IsSupported)
            {
                _service.Warning("External terminal is not supported on this platform.");
                return;
            }

            // クラッシュ等で後始末が走らなかった過去のセッションを片付ける
            // (生存しているプロセスのものは触らないため、他のUnityインスタンスを壊さない).
            CommandLineRelayScriptWriter.CleanUpStaleSessionDirectories();

            CommandLineBridge bridge = null;

            try
            {
                // TcpListener.Start()(ポート確保)もLaunch()(プロセス起動)と同じ失敗経路で
                // 扱う(片方だけtry外だと、ポート確保失敗時とプロセス起動失敗時とで
                // 後始末の一貫性が崩れるため).
                bridge = new CommandLineBridge(_service);

                // 自動起動の有無に関わらず中継スクリプトとトークンは書き出す
                // (利用者が手で接続するために必要なため).
                var connectionCommand = _launcher.BuildConnectionCommand(bridge.Port, bridge.Token);
                bridge.ConnectionHint = BuildHint(connectionCommand);

                ConnectionCommand = connectionCommand;

                if (_launchExternalTerminal)
                {
                    _process = _launcher.Launch(bridge.Port, bridge.Token);
                }

                _bridge = bridge;
            }
            catch (Exception e)
            {
                _service.Exception($"Failed to launch external terminal: {e}");
                bridge?.Dispose();
            }
        }

        /// <summary>
        /// 接続してきたクライアントへ見せる案内文を組み立てる.
        /// </summary>
        /// <remarks>
        /// 2つ目以降のターミナルを開くときに、ここからコマンドラインをコピーできるようにする.
        /// </remarks>
        private static string BuildHint(string connectionCommand) =>
            string.IsNullOrEmpty(connectionCommand)
                ? null
                : $"To open another terminal, run:\n  {connectionCommand}";

        /// <summary>
        /// 中継を終了する. <see cref="Open"/>で自動起動した外部ターミナルのウィンドウは
        /// ここで閉じる。<see cref="ConnectionCommand"/>の案内から利用者が手動で開いた
        /// ターミナルは対象外(<see cref="ICommandLineLauncher.CloseLaunchedTerminal"/>参照).
        /// </summary>
        /// <remarks>
        /// 破棄順序に意味がある。
        /// <list type="number">
        /// <item>
        /// 先に<see cref="_bridge"/>を破棄してソケットを閉じる。中継スクリプト(bash)は
        /// プロンプト応答後、次のユーザー入力を標準入力から待つため(ソケットとは別fd)、
        /// ソケットを閉じただけでは中継スクリプトが気づけない場合が多く(=実行中扱いのまま)、
        /// Terminal.appは「実行中のプロセスを終了しますか?」という確認ダイアログを出す。
        /// これ自体は<see cref="ICommandLineLauncher"/>実装側(<c>MacCommandLineLauncher</c>)が
        /// 確認ダイアログを自動で処理するため必須の前提ではないが、ソケットを閉じておくことで
        /// 中継スクリプトの状態を極力早くクリーンな側へ寄せておく意味はある(実機で確認済み).
        /// </item>
        /// <item>
        /// 次に<see cref="ICommandLineLauncher.CloseLaunchedTerminal"/>を呼ぶ。
        /// <c>WindowsCommandLineLauncher</c>は<see cref="Launch"/>が返したのと同じ
        /// <see cref="Process"/>参照(<see cref="_process"/>)を保持していて、これの
        /// <c>HasExited</c>判定・<c>Kill()</c>に使う。<see cref="_process"/>を先に
        /// <c>Dispose()</c>してしまうと、破棄済みリソースへアクセスすることになり
        /// 正しく終了できなくなる(<c>cmd.exe</c>が残ったままになる)ため、必ずこれより後で
        /// <see cref="_process"/>を破棄する.
        /// </item>
        /// <item>
        /// 最後に<see cref="_process"/>を破棄する.
        /// </item>
        /// </list>
        /// </remarks>
        public void Close()
        {
            _bridge?.Dispose();
            _bridge = null;

            _launcher.CloseLaunchedTerminal();

            _process?.Dispose();
            _process = null;

            // トークンファイルの後始末は中継スクリプト側では行わない(削除すると2つ目以降の
            // ターミナルが接続できなくなるため)。ここでセッションディレクトリごと片付ける.
            CommandLineRelayScriptWriter.DeleteSessionDirectory();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Close();
        }
    }
}

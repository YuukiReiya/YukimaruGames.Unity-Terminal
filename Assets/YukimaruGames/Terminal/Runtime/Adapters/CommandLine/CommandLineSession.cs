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

        private CommandLineBridge _bridge;
        private Process _process;
        private bool _disposed;

        /// <summary>
        /// 外部ターミナルが接続待ち/接続中か.
        /// </summary>
        public bool IsOpen => _bridge != null;

        public CommandLineSession(ITerminalService service, ICommandLineLauncher launcher = null)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _launcher = launcher ?? CreatePlatformLauncher();
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

            CommandLineBridge bridge = null;

            try
            {
                // TcpListener.Start()(ポート確保)もLaunch()(プロセス起動)と同じ失敗経路で
                // 扱う(片方だけtry外だと、ポート確保失敗時とプロセス起動失敗時とで
                // 後始末の一貫性が崩れるため).
                bridge = new CommandLineBridge(_service);
                _process = _launcher.Launch(bridge.Port, bridge.Token);
                _bridge = bridge;
            }
            catch (Exception e)
            {
                _service.Exception($"Failed to launch external terminal: {e}");
                bridge?.Dispose();
            }
        }

        /// <summary>
        /// 中継を終了する. 外部ターミナルのウィンドウ自体は(ユーザーの作業を強制的に
        /// 奪わないよう)強制終了しない。接続が切れた中継スクリプトは自然に入力待ちへ戻る.
        /// </summary>
        public void Close()
        {
            _process?.Dispose();
            _process = null;

            _bridge?.Dispose();
            _bridge = null;
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

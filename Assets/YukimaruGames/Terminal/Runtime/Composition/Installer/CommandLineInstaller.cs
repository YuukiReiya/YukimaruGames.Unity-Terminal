using System;
using UnityEngine;
using YukimaruGames.Terminal.Adapters.CommandLine;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Presentation.Contracts;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// CMD/zsh等の外部ターミナルプロセスを起動し、そこへコマンド入出力を中継するInstaller.
    /// </summary>
    /// <remarks>
    /// <see cref="ImmediateModeInstaller"/>がIMGUIレンダリング一式(Renderer/Presenter/Coordinator/
    /// TerminalIMGUI)を構築するのに対し、こちらはコマンド実行系(Domain/Application層)のみを構築し、
    /// 描画は行わない(ゲーム内ウィンドウを持たないため<see cref="ITerminalView"/>はNull実装を使う)。
    /// 「どちらのViewを使うか」は<see cref="TerminalBootstrapper"/>の_installerフィールド
    /// (SerializeReferenceの型選択メニュー)で切り替える想定。
    ///
    /// 描画を持たないため<see cref="GraphicalInstallerBase"/>ではなく<see cref="InstallerBase"/>を
    /// 直接継承し、<see cref="BuildBackend"/>で外部ターミナルのセッションを開くだけにしている(#145).
    /// </remarks>
    [Serializable, AddTypeMenu("CLI(cmd,zsh)")]
    public sealed class CommandLineInstaller : InstallerBase
    {
        [NonSerialized] private CommandLineSession _session;

        /// <summary>
        /// <see cref="CommandLineInstaller"/>を構築する.
        /// </summary>
        /// <remarks>
        /// 基底の既定値(<see cref="ImmediateModeOptions"/>)ではなく専用設定を既定にする。
        /// C#はコンストラクタ本体が基底のフィールド初期化子より後に実行されるため、ここでの代入が勝つ.
        /// </remarks>
        public CommandLineInstaller() => Options = new CommandLineOptions();

        /// <inheritdoc/>
        /// <remarks>
        /// Unity Editorの仕様上、SerializeReferenceな_installerフィールドの型をInspector上の
        /// 型選択メニューで切り替えた直後は、ネストした_optionsフィールドがユーザーの操作意図に
        /// 反してnullのまま復元されることがある(既知のシリアライズ上の癖。実際に検証で再現した)。
        /// <see cref="NullOptions"/>(BufferSize=0)へフォールバックするとCommandLoggerの実効バッファが
        /// 1件まで縮んで外部ターミナルとして機能しなくなるため、フォールバック先も
        /// 専用設定(<see cref="CommandLineOptions"/>)の既定値にする.
        /// </remarks>
        protected override ITerminalOptions CreateFallbackOptions() => new CommandLineOptions();

        /// <inheritdoc/>
        protected override BackendContext BuildBackend(ITerminalOptions options, in DomainContext domain)
        {
            // 自動起動の設定はCLI専用のため、共通のITerminalOptionsには持たせていない.
            var launchExternalTerminal = options is not CommandLineOptions commandLineOptions
                                         || commandLineOptions.LaunchExternalTerminal;

            var session = new CommandLineSession(domain.Service, launchExternalTerminal: launchExternalTerminal);
            session.Open();

            if (!launchExternalTerminal) NotifyConnectionCommand(session.ConnectionCommand);
            _session = session;

            return new BackendContext
            {
                // 描画を持たないため、OnGUIから駆動するGUIは無い.
                GUI = null,
                View = new NullTerminalView(),
                Components = new object[] { session },
            };
        }

        /// <summary>
        /// 手動接続用のコマンドラインを、利用者が実行できる形で渡す.
        /// </summary>
        /// <remarks>
        /// 自動起動しない場合、接続前のクライアントにはログが届かず過去ログも送られないため、
        /// 繋ぐための情報を得る手段が他に無い。長いパスを含む文字列を手で写させないよう
        /// クリップボードへ入れ、そのまま貼り付けて実行できるようにする。
        /// <para>
        /// コンソールへの出力はエディタでのみ行う。ビルドしたプレイヤーではコンソールが無く、
        /// ログを出しても利用者が読む手段が無いため(クリップボードは実機でも機能する)。
        /// </para>
        /// <para>
        /// <c>Adapters.CommandLine</c>はUnity非依存(<c>noEngineReferences</c>)のため、
        /// UnityEngineに触れるこの処理はComposition層側で行う(#160).
        /// </para>
        /// </remarks>
        private static void NotifyConnectionCommand(string connectionCommand)
        {
            if (string.IsNullOrEmpty(connectionCommand))
            {
                // 中継スクリプトを書き出せなかった場合。接続する手段が無いことを知らせる
                // (エラー経路のため、コンソールが無いプレイヤーでもログへ残す).
                Debug.LogWarning(
                    "[YukimaruGames.Terminal] Failed to prepare the relay script. "
                    + "No external terminal can connect to this session.");
                return;
            }

            GUIUtility.systemCopyBuffer = connectionCommand;

#if UNITY_EDITOR
            Debug.Log(
                "[YukimaruGames.Terminal] Copied to clipboard. Paste this into your terminal to connect:\n"
                + connectionCommand);
#endif
        }

        /// <inheritdoc/>
        protected override void ClearReferences()
        {
            _session = null;

            base.ClearReferences();
        }
    }
}

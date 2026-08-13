using System;
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
    /// 描画を持たないため<see cref="RenderingInstallerBase"/>ではなく<see cref="InstallerBase"/>を
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
            var session = new CommandLineSession(domain.Service);
            session.Open();
            _session = session;

            return new BackendContext
            {
                // 描画を持たないため、OnGUIから駆動するGUIは無い.
                GUI = null,
                View = new NullTerminalView(),
                Components = new object[] { session },
            };
        }

        /// <inheritdoc/>
        protected override void ClearReferences()
        {
            _session = null;

            base.ClearReferences();
        }
    }
}

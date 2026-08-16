using System.Reflection;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Presentation.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// キー操作やボタンに手が届かない構成のための、逃げ道となる組み込みコマンド.
    /// </summary>
    /// <remarks>
    /// 入力欄がフォーカスを持っている間はキー入力による閉じる操作を抑止しているため(#149)、
    /// フォーカスを外す手段が要る。通常は入力欄の外をクリックすればよいが、モバイルなど
    /// ウィンドウが画面全体を覆う構成ではクリックできる余白もランチャーボタンも無く、
    /// 物理キーボードが無ければ閉じるキーも押せない。コマンド実行はフォーカス中でも動くため、
    /// 確実な逃げ道になる。
    /// <para>
    /// 対になる「フォーカスを当てる」「ウィンドウを開く」コマンドは用意しない。
    /// コマンドを打てている時点で入力欄はフォーカスを持ち、ウィンドウは開いているため、
    /// いずれも意味を持たない。
    /// </para>
    /// <para>
    /// 他の組み込みコマンド(<c>BuiltinDiagnosticsCommands</c>等)はInfrastructure層のstaticメソッドだが、
    /// これらはPresentation層に触れる必要があるためそちらには置けない
    /// (InfrastructureはPresentationを参照できない)。両方を知っているComposition層に置く.
    /// </para>
    /// </remarks>
    internal sealed class TerminalWindowCommands
    {
        internal const string UnfocusCommand = "terminal.unfocus";
        internal const string CloseCommand = "terminal.close";

        private const string UnfocusHelp =
            "Releases focus from the input field. Key bindings such as the close key are suppressed " +
            "while the input field has focus, so use this to get them back when the window covers " +
            "the whole screen and there is nowhere to click.";

        private const string CloseHelp =
            "Closes the terminal window. Use this when the launcher button is out of reach and no " +
            "physical keyboard is available to press the close key.";

        private readonly IInputPresenter _inputPresenter;
        private readonly TerminalCoordinator _coordinator;

        internal TerminalWindowCommands(IInputPresenter inputPresenter, TerminalCoordinator coordinator)
        {
            _inputPresenter = inputPresenter;
            _coordinator = coordinator;
        }

        internal static CommandMeta UnfocusMeta { get; } = new(UnfocusCommand, 0, 0, UnfocusHelp);
        internal static CommandMeta CloseMeta { get; } = new(CloseCommand, 0, 0, CloseHelp);

        internal static MethodInfo UnfocusMethod { get; } =
            typeof(TerminalWindowCommands).GetMethod(
                nameof(Unfocus), BindingFlags.NonPublic | BindingFlags.Instance)!;

        internal static MethodInfo CloseMethod { get; } =
            typeof(TerminalWindowCommands).GetMethod(
                nameof(Close), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private void Unfocus() => _inputPresenter?.SetFocus(false);

        /// <remarks>
        /// 閉じるキー・ランチャーボタンと同じ経路を通す。フォーカスの解放と入力ガードの解除まで
        /// 含めるため、ウィンドウの表示状態だけを変える<c>ITerminalView.SetVisible(false)</c>では
        /// 代替できない.
        /// </remarks>
        private void Close() => _coordinator?.RequestClose();
    }
}

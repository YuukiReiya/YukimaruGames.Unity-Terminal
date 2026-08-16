using System.Reflection;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 入力欄のフォーカスを操作する組み込みコマンド.
    /// </summary>
    /// <remarks>
    /// 他の組み込みコマンド(<c>BuiltinDiagnosticsCommands</c>等)はInfrastructure層のstaticメソッドだが、
    /// これらはPresentation層の<see cref="IInputPresenter"/>に触れる必要があるためそちらには置けない
    /// (Infrastructureは Presentation を参照できない)。両方を知っているComposition層に置く。
    /// <para>
    /// 対になる「フォーカスを当てる」コマンドは用意しない。コマンドを打てている時点で入力欄は
    /// フォーカスを持っているため、意味を持たない.
    /// </para>
    /// </remarks>
    internal sealed class InputFocusCommands
    {
        internal const string UnfocusCommand = "terminal.unfocus";

        private const string UnfocusHelp =
            "Releases focus from the input field. Key bindings such as the close key are suppressed " +
            "while the input field has focus, so use this to get them back when the window covers " +
            "the whole screen and there is nowhere to click.";

        private readonly IInputPresenter _inputPresenter;

        internal InputFocusCommands(IInputPresenter inputPresenter) => _inputPresenter = inputPresenter;

        /// <summary>
        /// このクラスが提供するコマンドのメタ情報.
        /// </summary>
        internal static CommandMeta UnfocusMeta { get; } = new(UnfocusCommand, 0, 0, UnfocusHelp);

        /// <summary>
        /// <see cref="Unfocus"/>のメソッド情報.
        /// </summary>
        internal static MethodInfo UnfocusMethod { get; } =
            typeof(InputFocusCommands).GetMethod(
                nameof(Unfocus), BindingFlags.NonPublic | BindingFlags.Instance)!;

        private void Unfocus() => _inputPresenter?.SetFocus(false);
    }
}

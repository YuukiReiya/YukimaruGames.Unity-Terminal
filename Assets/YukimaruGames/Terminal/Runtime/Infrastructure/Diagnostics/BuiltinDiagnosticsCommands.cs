using System.Reflection;
using System.Text;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Infrastructure.Diagnostics
{
    /// <summary>
    /// モードスタックの状態を確認するための診断コマンド群.
    /// </summary>
    /// <remarks>
    /// <p>
    /// モードの内部状態には一切触れない、型名・識別子・深さのみのメタ情報を表示する.
    /// </p>
    /// <p>
    /// パッケージ内蔵コマンドは <see cref="Infrastructure.Discoverer.CommandDiscoverer"/> による
    /// アセンブリ走査(既定では利用者の <c>Assembly-CSharp</c>とその参照先のみ)に乗らない場合がある
    /// (利用者コードがこのアセンブリの型を実際に参照していないと、コンパイル後の参照メタデータに
    /// このアセンブリが現れないため)。そのため<see cref="Methods"/>経由でComposition層から直接登録する.
    /// </p>
    /// </remarks>
    public static class BuiltinDiagnosticsCommands
    {
        private const string StackCommand = "terminal.stack";
        private const string StackHelp = "Prints the current terminal mode stack.";

        [TerminalCommand(StackCommand, help: StackHelp)]
        private static void PrintModeStack(IModeStackInspector stack, IModeOutput output)
        {
            var frames = stack.Snapshot();
            var builder = new StringBuilder();

            for (var i = frames.Count - 1; i >= 0; i--)
            {
                var frame = frames[i];
                builder.Append('[').Append(frame.Depth).Append("] ")
                    .Append(frame.Id).Append(" (").Append(frame.TypeName).Append(')')
                    .Append(i == 0 ? string.Empty : "\n");
            }

            output.Message(builder.ToString());
        }

        /// <summary>
        /// このクラスが提供するコマンドメソッド一覧.
        /// </summary>
        public static MethodInfo[] Methods { get; } =
        {
            typeof(BuiltinDiagnosticsCommands).GetMethod(
                nameof(PrintModeStack), BindingFlags.NonPublic | BindingFlags.Static)!,
        };
    }
}

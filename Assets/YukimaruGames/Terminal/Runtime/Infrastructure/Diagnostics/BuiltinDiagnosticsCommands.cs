using System.Reflection;
using System.Text;
using UnityEngine.Scripting;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
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
    /// パッケージ内蔵コマンドは<c>[TerminalCommand]</c>属性を付与しない。自動探索
    /// (<c>CommandDiscoverer</c>)の走査範囲拡張(#176/#177)後、属性を付けると利用者コードの
    /// 明示登録(<see cref="Commands"/>経由、Composition層)と二重登録になるため。
    /// リフレクションでのみ参照されるため、コード剥離(IL2CPPストリッピング)で消えないよう
    /// <see cref="PreserveAttribute"/>を付与している.
    /// </p>
    /// </remarks>
    public static class BuiltinDiagnosticsCommands
    {
        private const string StackCommand = "terminal.stack";
        private const string StackHelp = "Prints the current terminal mode stack.";

        [Preserve]
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
        /// このクラスが提供するコマンドメソッドとメタ情報の一覧.
        /// </summary>
        public static (MethodInfo Method, CommandMeta Meta)[] Commands { get; } =
        {
            (
                typeof(BuiltinDiagnosticsCommands).GetMethod(
                    nameof(PrintModeStack), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(StackCommand, maxArgCount: 0, minArgCount: -1, help: StackHelp)
            ),
        };
    }
}

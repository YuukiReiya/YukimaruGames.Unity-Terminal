using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Infrastructure.Diagnostics
{
    /// <summary>
    /// 特定アプリに依存しない、汎用的な診断・ユーティリティ用の組み込みコマンド群.
    /// </summary>
    /// <remarks>
    /// <see cref="BuiltinDiagnosticsCommands"/>と同様、パッケージ内蔵コマンドは
    /// <c>[TerminalCommand]</c>属性を付与しない(自動探索との二重登録を避けるため)。
    /// <see cref="Commands"/>経由でComposition層から直接登録し、
    /// <see cref="PreserveAttribute"/>でコード剥離から保護する.
    /// </remarks>
    public static class BuiltinGeneralCommands
    {
        private const string EchoCommand = "echo";
        private const int EchoMaxArgCount = 64;
        private const string EchoHelp = "Echoes the given arguments back. Usage: echo [text...]";

        [Preserve]
        private static void Echo(CommandArgument[] args, IModeOutput output)
        {
            output.Message(args.Length == 0 ? string.Empty : string.Join(' ', args.Select(a => a.String)));
        }

        private const string CommandsCommand = "commands";
        private const string CommandsHelp = "Lists all registered commands with their help text.";

        /// <summary>領域名とコマンド名を区切る文字.</summary>
        private const char GroupSeparator = '.';

        /// <summary>領域に属するコマンドの字下げ.</summary>
        private const string GroupIndent = "  ";

        /// <summary>領域名の表示色.</summary>
        private const string GroupColor = "#7fdbff";

        /// <summary>コマンド名の表示色.</summary>
        private const string CommandColor = "#a6e22e";

        /// <summary>
        /// 登録済みコマンドの一覧を、領域ごとにまとめて表示する.
        /// </summary>
        /// <remarks>
        /// 色はUnityのリッチテキストタグで表現する。グラフィカルなバックエンドはそのまま解釈し、
        /// CLIバックエンドは出力時にANSIエスケープへ変換する(#156)。
        /// <para>
        /// 色はここで定数として持ち、テーマ(<c>ITerminalTheme</c>)には連動させない。
        /// 診断系の組み込みコマンドであり、Infrastructure層からはテーマを参照できないため.
        /// </para>
        /// </remarks>
        [Preserve]
        private static void ListCommands(ICommandRegistry registry, IModeOutput output)
        {
            var commands = registry.All.Select(h => h.Meta).ToArray();

            if (commands.Length == 0)
            {
                output.Message("No commands are registered.");
                return;
            }

            // 領域名の集合。ドットを持つコマンドの接頭辞だけが領域になる.
            var groups = new HashSet<string>(StringComparer.Ordinal);
            foreach (var meta in commands)
            {
                var separatorIndex = meta.Command.IndexOf(GroupSeparator);
                if (separatorIndex > 0) groups.Add(meta.Command[..separatorIndex]);
            }

            var lines = new List<string>();

            // 無所属のコマンドを先頭にまとめる.
            foreach (var meta in commands
                         .Where(m => ResolveGroup(m.Command, groups) == null)
                         .OrderBy(m => m.Command, StringComparer.Ordinal))
            {
                lines.Add(FormatEntry(meta, string.Empty));
            }

            foreach (var group in groups.OrderBy(g => g, StringComparer.Ordinal))
            {
                // ブロック間に空行を挟む(先頭ブロックの前には入れない).
                if (lines.Count > 0) lines.Add(string.Empty);

                lines.Add($"<color={GroupColor}>{group}</color>");

                foreach (var meta in commands
                             .Where(m => ResolveGroup(m.Command, groups) == group)
                             .OrderBy(m => m.Command, StringComparer.Ordinal))
                {
                    lines.Add(FormatEntry(meta, GroupIndent));
                }
            }

            output.Message(string.Join("\n", lines));
        }

        /// <summary>
        /// コマンドが属する領域名を返す。無所属の場合は<c>null</c>.
        /// </summary>
        /// <remarks>
        /// ドットを持つものは接頭辞が領域になる。ドットを持たないものは、同名の領域が存在する場合だけ
        /// そこへ属させる(<c>fps</c>は<c>fps.set</c>があるため<c>fps</c>領域。<c>echo</c>は無所属)。
        /// こうしないと<c>fps</c>と<c>fps.set</c>が離れて並び、探しにくくなる.
        /// </remarks>
        private static string ResolveGroup(string command, HashSet<string> groups)
        {
            var separatorIndex = command.IndexOf(GroupSeparator);
            if (separatorIndex > 0) return command[..separatorIndex];

            return groups.Contains(command) ? command : null;
        }

        /// <summary>1コマンド分の行を組み立てる.</summary>
        private static string FormatEntry(in CommandMeta meta, string indent)
        {
            var entry = $"{indent}<color={CommandColor}>{meta.Command}</color>";

            return string.IsNullOrEmpty(meta.Help) ? entry : $"{entry} - {meta.Help}";
        }

        private const string TimeScaleCommand = "time.scale";
        private const string TimeScaleHelp = "Gets or sets Time.timeScale. Usage: time.scale [value]";

        [Preserve]
        private static void TimeScale(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length == 0)
            {
                output.Message($"Time.timeScale = {Time.timeScale}");
                return;
            }

            var value = args[0].Float;
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                output.Error($"{TimeScaleCommand} requires a finite value >= 0.");
                return;
            }

            Time.timeScale = value;
            output.Message($"Time.timeScale = {Time.timeScale}");
        }

        private const string FpsCommand = "fps";
        private const string FpsHelp = "Prints the current frame rate.";
        private const float MillisecondsPerSecond = 1000f;

        [Preserve]
        private static void PrintFps(IModeOutput output)
        {
            var deltaTime = Time.unscaledDeltaTime;
            var fps = deltaTime > 0f ? 1f / deltaTime : 0f;
            output.Message($"{fps:F1} fps ({deltaTime * MillisecondsPerSecond:F2} ms)");
        }

        private const string FpsSetCommand = "fps.set";
        private const string FpsSetHelp = "Sets Application.targetFrameRate. Usage: fps.set <value> (-1 = unlimited)";
        private const int UnlimitedFrameRate = -1;

        [Preserve]
        private static void SetTargetFrameRate(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length < 1)
            {
                output.Error($"Usage: {FpsSetCommand} <value> ({UnlimitedFrameRate} = unlimited)");
                return;
            }

            var value = args[0].Int;
            if (value < UnlimitedFrameRate || value == 0)
            {
                output.Error($"{FpsSetCommand} requires {UnlimitedFrameRate} (unlimited) or a positive value.");
                return;
            }

            Application.targetFrameRate = value;
            output.Message($"Application.targetFrameRate = {Application.targetFrameRate}");
        }

        private const string QualityListCommand = "quality.list";
        private const string QualityListHelp = "Lists the available QualitySettings levels.";

        [Preserve]
        private static void ListQualityLevels(IModeOutput output)
        {
            var names = QualitySettings.names;
            var current = QualitySettings.GetQualityLevel();
            var builder = new StringBuilder();

            for (var i = 0; i < names.Length; i++)
            {
                builder.Append('[').Append(i).Append("] ").Append(names[i])
                    .Append(i == current ? " (current)" : string.Empty)
                    .Append(i == names.Length - 1 ? string.Empty : "\n");
            }

            output.Message(builder.ToString());
        }

        private const string QualitySetCommand = "quality.set";
        private const string QualitySetHelp = "Sets the QualitySettings level by index. Usage: quality.set <index>";

        [Preserve]
        private static void SetQualityLevel(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length < 1)
            {
                output.Error($"Usage: {QualitySetCommand} <index>");
                return;
            }

            var index = args[0].Int;
            var names = QualitySettings.names;
            if (index < 0 || index >= names.Length)
            {
                output.Error($"Quality level index must be between 0 and {names.Length - 1}.");
                return;
            }

            QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
            output.Message($"Quality level set to [{index}] {names[index]}.");
        }

        private const string ClearCommand = "clear";
        private const string ClearHelp = "Clears all terminal logs.";

        [Preserve]
        private static void Clear(ICommandLogger logger)
        {
            logger.Clear();
        }

        private const string GcCollectCommand = "gc.collect";
        private const string GcCollectHelp = "Forces an immediate garbage collection.";
        private const float BytesPerKilobyte = 1024f;

        [Preserve]
        private static void ForceGarbageCollect(IModeOutput output)
        {
            var before = GC.GetTotalMemory(forceFullCollection: false);
            GC.Collect();
            var after = GC.GetTotalMemory(forceFullCollection: false);
            output.Message($"GC.Collect() done. {before / BytesPerKilobyte:F1} KB -> {after / BytesPerKilobyte:F1} KB");
        }

        /// <summary>
        /// このクラスが提供するコマンドメソッドとメタ情報の一覧.
        /// </summary>
        public static (MethodInfo Method, CommandMeta Meta)[] Commands { get; } =
        {
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(EchoCommand, maxArgCount: EchoMaxArgCount, minArgCount: 0, help: EchoHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(ListCommands), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(CommandsCommand, maxArgCount: 0, minArgCount: -1, help: CommandsHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(TimeScale), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(TimeScaleCommand, maxArgCount: 1, minArgCount: 0, help: TimeScaleHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(PrintFps), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(FpsCommand, maxArgCount: 0, minArgCount: -1, help: FpsHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(SetTargetFrameRate), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(FpsSetCommand, maxArgCount: 1, minArgCount: 1, help: FpsSetHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(ListQualityLevels), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(QualityListCommand, maxArgCount: 0, minArgCount: -1, help: QualityListHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(SetQualityLevel), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(QualitySetCommand, maxArgCount: 1, minArgCount: 1, help: QualitySetHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(Clear), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(ClearCommand, maxArgCount: 0, minArgCount: -1, help: ClearHelp)
            ),
            (
                typeof(BuiltinGeneralCommands).GetMethod(nameof(ForceGarbageCollect), BindingFlags.NonPublic | BindingFlags.Static)!,
                new CommandMeta(GcCollectCommand, maxArgCount: 0, minArgCount: -1, help: GcCollectHelp)
            ),
        };
    }
}

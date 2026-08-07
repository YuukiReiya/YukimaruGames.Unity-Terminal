using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
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
    /// <c>CommandDiscoverer</c>によるアセンブリ走査に乗らない場合があるため、
    /// Composition層から<see cref="Methods"/>経由で直接登録する.
    /// </remarks>
    public static class BuiltinGeneralCommands
    {
        private const string EchoCommand = "echo";
        private const int EchoMaxArgCount = 64;
        private const string EchoHelp = "Echoes the given arguments back. Usage: echo [text...]";

        [TerminalCommand(EchoCommand, maxArgCount: EchoMaxArgCount, minArgCount: 0, help: EchoHelp)]
        private static void Echo(CommandArgument[] args, IModeOutput output)
        {
            output.Message(args.Length == 0 ? string.Empty : string.Join(' ', args.Select(a => a.String)));
        }

        private const string CommandsCommand = "commands";
        private const string CommandsHelp = "Lists all registered commands with their help text.";

        [TerminalCommand(CommandsCommand, help: CommandsHelp)]
        private static void ListCommands(ICommandRegistry registry, IModeOutput output)
        {
            var handlers = registry.All
                .OrderBy(h => h.Meta.Command, StringComparer.Ordinal)
                .ToArray();

            if (handlers.Length == 0)
            {
                output.Message("No commands are registered.");
                return;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < handlers.Length; i++)
            {
                var meta = handlers[i].Meta;
                builder.Append(meta.Command);
                if (!string.IsNullOrEmpty(meta.Help))
                {
                    builder.Append(" - ").Append(meta.Help);
                }

                builder.Append(i == handlers.Length - 1 ? string.Empty : "\n");
            }

            output.Message(builder.ToString());
        }

        private const string TimeScaleCommand = "time.scale";
        private const string TimeScaleHelp = "Gets or sets Time.timeScale. Usage: time.scale [value]";

        [TerminalCommand(TimeScaleCommand, maxArgCount: 1, minArgCount: 0, help: TimeScaleHelp)]
        private static void TimeScale(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length == 0)
            {
                output.Message($"Time.timeScale = {Time.timeScale}");
                return;
            }

            var value = args[0].Float;
            if (value < 0f)
            {
                output.Error($"{TimeScaleCommand} requires a value >= 0.");
                return;
            }

            Time.timeScale = value;
            output.Message($"Time.timeScale = {Time.timeScale}");
        }

        private const string FpsCommand = "fps";
        private const string FpsHelp = "Prints the current frame rate.";
        private const float MillisecondsPerSecond = 1000f;

        [TerminalCommand(FpsCommand, help: FpsHelp)]
        private static void PrintFps(IModeOutput output)
        {
            var deltaTime = Time.unscaledDeltaTime;
            var fps = deltaTime > 0f ? 1f / deltaTime : 0f;
            output.Message($"{fps:F1} fps ({deltaTime * MillisecondsPerSecond:F2} ms)");
        }

        private const string FpsSetCommand = "fps.set";
        private const string FpsSetHelp = "Sets Application.targetFrameRate. Usage: fps.set <value> (-1 = unlimited)";
        private const int UnlimitedFrameRate = -1;

        [TerminalCommand(FpsSetCommand, maxArgCount: 1, minArgCount: 1, help: FpsSetHelp)]
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

        [TerminalCommand(QualityListCommand, help: QualityListHelp)]
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

        [TerminalCommand(QualitySetCommand, maxArgCount: 1, minArgCount: 1, help: QualitySetHelp)]
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

        private const string GcCollectCommand = "gc.collect";
        private const string GcCollectHelp = "Forces an immediate garbage collection.";
        private const float BytesPerKilobyte = 1024f;

        [TerminalCommand(GcCollectCommand, help: GcCollectHelp)]
        private static void ForceGarbageCollect(IModeOutput output)
        {
            var before = GC.GetTotalMemory(forceFullCollection: false);
            GC.Collect();
            var after = GC.GetTotalMemory(forceFullCollection: false);
            output.Message($"GC.Collect() done. {before / BytesPerKilobyte:F1} KB -> {after / BytesPerKilobyte:F1} KB");
        }

        /// <summary>
        /// このクラスが提供するコマンドメソッド一覧.
        /// </summary>
        public static MethodInfo[] Methods { get; } =
        {
            typeof(BuiltinGeneralCommands).GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(ListCommands), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(TimeScale), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(PrintFps), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(SetTargetFrameRate), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(ListQualityLevels), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(SetQualityLevel), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(BuiltinGeneralCommands).GetMethod(nameof(ForceGarbageCollect), BindingFlags.NonPublic | BindingFlags.Static)!,
        };
    }
}

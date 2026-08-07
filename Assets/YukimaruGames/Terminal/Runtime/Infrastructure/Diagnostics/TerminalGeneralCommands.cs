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
    /// <see cref="TerminalModeDiagnosticsCommands"/>と同様、パッケージ内蔵コマンドは
    /// <c>CommandDiscoverer</c>によるアセンブリ走査に乗らない場合があるため、
    /// Composition層から<see cref="Methods"/>経由で直接登録する.
    /// </remarks>
    public static class TerminalGeneralCommands
    {
        [TerminalCommand("echo", maxArgCount: 64, minArgCount: 0, help: "Echoes the given arguments back. Usage: echo [text...]")]
        private static void Echo(CommandArgument[] args, IModeOutput output)
        {
            output.Message(args.Length == 0 ? string.Empty : string.Join(' ', args.Select(a => a.String)));
        }

        [TerminalCommand("commands", help: "Lists all registered command names.")]
        private static void ListCommands(ICommandAutocomplete autocomplete, IModeOutput output)
        {
            var names = autocomplete.KnownWords.OrderBy(n => n, StringComparer.Ordinal).ToArray();
            if (names.Length == 0)
            {
                output.Message("No commands are registered.");
                return;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < names.Length; i++)
            {
                builder.Append(names[i]).Append(i == names.Length - 1 ? string.Empty : "\n");
            }

            output.Message(builder.ToString());
        }

        [TerminalCommand("time.scale", maxArgCount: 1, minArgCount: 0, help: "Gets or sets Time.timeScale. Usage: time.scale [value]")]
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
                output.Error("time.scale requires a value >= 0.");
                return;
            }

            Time.timeScale = value;
            output.Message($"Time.timeScale = {Time.timeScale}");
        }

        [TerminalCommand("fps", help: "Prints the current frame rate.")]
        private static void PrintFps(IModeOutput output)
        {
            var deltaTime = Time.unscaledDeltaTime;
            var fps = deltaTime > 0f ? 1f / deltaTime : 0f;
            output.Message($"{fps:F1} fps ({deltaTime * 1000f:F2} ms)");
        }

        [TerminalCommand("fps.set", maxArgCount: 1, minArgCount: 1, help: "Sets Application.targetFrameRate. Usage: fps.set <value> (-1 = unlimited)")]
        private static void SetTargetFrameRate(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length < 1)
            {
                output.Error("Usage: fps.set <value> (-1 = unlimited)");
                return;
            }

            var value = args[0].Int;
            if (value < -1 || value == 0)
            {
                output.Error("fps.set requires -1 (unlimited) or a positive value.");
                return;
            }

            Application.targetFrameRate = value;
            output.Message($"Application.targetFrameRate = {Application.targetFrameRate}");
        }

        [TerminalCommand("quality.list", help: "Lists the available QualitySettings levels.")]
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

        [TerminalCommand("quality.set", maxArgCount: 1, minArgCount: 1, help: "Sets the QualitySettings level by index. Usage: quality.set <index>")]
        private static void SetQualityLevel(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length < 1)
            {
                output.Error("Usage: quality.set <index>");
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

        [TerminalCommand("gc.collect", help: "Forces an immediate garbage collection.")]
        private static void ForceGarbageCollect(IModeOutput output)
        {
            var before = GC.GetTotalMemory(forceFullCollection: false);
            GC.Collect();
            var after = GC.GetTotalMemory(forceFullCollection: false);
            output.Message($"GC.Collect() done. {before / 1024f:F1} KB -> {after / 1024f:F1} KB");
        }

        /// <summary>
        /// このクラスが提供するコマンドメソッド一覧.
        /// </summary>
        public static MethodInfo[] Methods { get; } =
        {
            typeof(TerminalGeneralCommands).GetMethod(nameof(Echo), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(ListCommands), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(TimeScale), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(PrintFps), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(SetTargetFrameRate), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(ListQualityLevels), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(SetQualityLevel), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalGeneralCommands).GetMethod(nameof(ForceGarbageCollect), BindingFlags.NonPublic | BindingFlags.Static)!,
        };
    }
}

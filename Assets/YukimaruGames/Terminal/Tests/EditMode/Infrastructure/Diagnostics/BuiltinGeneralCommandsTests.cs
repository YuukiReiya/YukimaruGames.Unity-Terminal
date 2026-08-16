using System;
using System.Collections.Generic;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;
using YukimaruGames.Terminal.Infrastructure.Diagnostics;
using YukimaruGames.Terminal.Infrastructure.Factories;

namespace YukimaruGames.Terminal.Tests.EditMode.Infrastructure.Diagnostics
{
    /// <summary>
    /// <see cref="BuiltinGeneralCommands"/> が提供する組み込みコマンドを検証するテストクラス.
    /// </summary>
    [TestFixture]
    public sealed class BuiltinGeneralCommandsTests
    {
        /// <summary>
        /// 出力を記録するだけの<see cref="IModeOutput"/>テストダブル.
        /// </summary>
        private sealed class RecordingOutput : IModeOutput
        {
            private readonly List<string> _messages = new();
            private readonly List<string> _errors = new();

            /// <summary>
            /// <see cref="Message"/>で記録されたメッセージ一覧.
            /// </summary>
            public IReadOnlyList<string> Messages => _messages;

            /// <summary>
            /// <see cref="Error"/>で記録されたエラー一覧.
            /// </summary>
            public IReadOnlyList<string> Errors => _errors;

            public void Message(string message) => _messages.Add(message);
            public void Warning(string message) { }
            public void Error(string message) => _errors.Add(message);
        }

        /// <summary>
        /// 固定のハンドラー一覧を返す<see cref="ICommandRegistry"/>テストダブル.
        /// </summary>
        private sealed class FixedRegistry : ICommandRegistry
        {
            /// <summary>
            /// <see cref="ICommandRegistry.All"/>として返す固定のハンドラー一覧.
            /// </summary>
            public IEnumerable<CommandHandler> All { get; set; } = Array.Empty<CommandHandler>();

            public bool Add(string command, CommandHandler handle) => true;
            public bool Remove(string command) => true;

            public bool TryGet(string command, out CommandHandler handler)
            {
                handler = default;
                return false;
            }
        }

        private static CommandHandler CreateHandler(string methodName, RecordingOutput output, FixedRegistry registry)
        {
            var method = typeof(BuiltinGeneralCommands).GetMethod(
                methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");

            var services = new Dictionary<Type, object>
            {
                { typeof(IModeOutput), output },
                { typeof(ICommandRegistry), registry },
            };

            return CommandFactory.Create(method, new ModeServiceBundle(services));
        }

        private static CommandHandler CreateHandler(string methodName, RecordingOutput output) =>
            CreateHandler(methodName, output, new FixedRegistry());

        private static CommandHandler MakeStubHandler(string command, string help) =>
            new((CommandDelegate)(_ => { }), command, minArgCount: 0, maxArgCount: 0, help);

        /// <summary>引数付きechoが、空白区切りで結合したメッセージを出力することを検証します.</summary>
        [Test]
        public void Echo_WithArguments_JoinsArgumentsWithSpace()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("Echo", output);

            handler.Proc(new CommandArgument[] { new("hello"), new("world") }.AsMemory());

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("hello world"));
        }

        /// <summary>引数無しechoが、空文字列のメッセージを出力することを検証します.</summary>
        [Test]
        public void Echo_WithoutArguments_PrintsEmptyMessage()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("Echo", output);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// 無所属のコマンドが先頭に、以降は領域ごとにまとめて出力されることを検証します.
        /// </summary>
        /// <remarks>
        /// ドット無しの<c>fps</c>は同名の領域(<c>fps.set</c>が作る)が存在するため、無所属ではなく
        /// <c>fps</c>領域へ入る。<c>echo</c>は該当領域が無いため無所属.
        /// </remarks>
        [Test]
        public void ListCommands_WithRegisteredHandlers_GroupsByPrefixAndSortsAlphabetically()
        {
            var output = new RecordingOutput();
            var registry = new FixedRegistry
            {
                All = new[]
                {
                    MakeStubHandler("fps.set", "Sets the frame rate."),
                    MakeStubHandler("echo", "Echoes text back."),
                    MakeStubHandler("commands", "Lists all registered commands."),
                    MakeStubHandler("fps", string.Empty),
                },
            };
            var handler = CreateHandler("ListCommands", output, registry);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));

            var lines = output.Messages[0].Split('\n');

            Assert.That(StripTags(lines[0]), Is.EqualTo("commands - Lists all registered commands."));
            Assert.That(StripTags(lines[1]), Is.EqualTo("echo - Echoes text back."));
            Assert.That(lines[2], Is.Empty, "ブロック間に空行が入る");
            Assert.That(StripTags(lines[3]), Is.EqualTo("fps"), "領域名の見出し");
            Assert.That(StripTags(lines[4]), Is.EqualTo("  fps"), "ドット無しでも同名領域へ入る");
            Assert.That(StripTags(lines[5]), Is.EqualTo("  fps.set - Sets the frame rate."));
            Assert.That(lines, Has.Length.EqualTo(6));
        }

        /// <summary>コマンド名と領域名がリッチテキストタグで色付けされることを検証します.</summary>
        [Test]
        public void ListCommands_WithRegisteredHandlers_ColorsGroupAndCommandNames()
        {
            var output = new RecordingOutput();
            var registry = new FixedRegistry
            {
                All = new[] { MakeStubHandler("fps.set", "Sets the frame rate."), MakeStubHandler("fps", string.Empty) },
            };
            var handler = CreateHandler("ListCommands", output, registry);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            var lines = output.Messages[0].Split('\n');

            Assert.That(lines[0], Does.StartWith("<color=").And.Contains("fps</color>"), "領域名に色が付く");
            Assert.That(lines[1], Does.Contain("<color=").And.Contains("fps</color>"), "コマンド名に色が付く");
            Assert.That(lines[2], Does.Contain("fps.set</color> - Sets the frame rate."), "説明には色を付けない");
        }

        /// <summary>アサート用に、リッチテキストタグを取り除きます.</summary>
        private static string StripTags(string line) =>
            System.Text.RegularExpressions.Regex.Replace(line, "<.*?>", string.Empty);

        /// <summary>登録済みハンドラーが無い場合、プレースホルダーメッセージを出力することを検証します.</summary>
        [Test]
        public void ListCommands_WithNoRegisteredHandlers_PrintsPlaceholderMessage()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("ListCommands", output);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("No commands are registered."));
        }

        /// <summary>負の値を指定した場合、エラーを報告しTime.timeScaleを変更しないことを検証します.</summary>
        [Test]
        public void TimeScale_WithNegativeValue_ReportsErrorWithoutApplying()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("TimeScale", output);
            var before = UnityEngine.Time.timeScale;

            handler.Proc(new CommandArgument[] { new("-1") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(UnityEngine.Time.timeScale, Is.EqualTo(before));
        }

        /// <summary>NaNを指定した場合、エラーを報告しTime.timeScaleを変更しないことを検証します.</summary>
        [Test]
        public void TimeScale_WithNaN_ReportsErrorWithoutApplying()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("TimeScale", output);
            var before = UnityEngine.Time.timeScale;

            handler.Proc(new CommandArgument[] { new("NaN") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(UnityEngine.Time.timeScale, Is.EqualTo(before));
        }

        /// <summary>Infinityを指定した場合、エラーを報告しTime.timeScaleを変更しないことを検証します.</summary>
        [Test]
        public void TimeScale_WithInfinity_ReportsErrorWithoutApplying()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("TimeScale", output);
            var before = UnityEngine.Time.timeScale;

            handler.Proc(new CommandArgument[] { new("Infinity") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(UnityEngine.Time.timeScale, Is.EqualTo(before));
        }

        /// <summary>正の値を指定した場合、Application.targetFrameRateに反映されることを検証します.</summary>
        [Test]
        public void SetTargetFrameRate_WithPositiveValue_AppliesValue()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetTargetFrameRate", output);
            var before = UnityEngine.Application.targetFrameRate;

            try
            {
                handler.Proc(new CommandArgument[] { new("30") }.AsMemory());

                Assert.That(output.Errors, Is.Empty);
                Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(30));
            }
            finally
            {
                UnityEngine.Application.targetFrameRate = before;
            }
        }

        /// <summary>0を指定した場合、エラーを報告しApplication.targetFrameRateを変更しないことを検証します.</summary>
        [Test]
        public void SetTargetFrameRate_WithZero_ReportsErrorWithoutApplying()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetTargetFrameRate", output);
            var before = UnityEngine.Application.targetFrameRate;

            handler.Proc(new CommandArgument[] { new("0") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(before));
        }

        /// <summary>引数無しでquality.setを実行した場合、使用方法エラーを報告することを検証します.</summary>
        [Test]
        public void SetQualityLevel_WithoutArguments_ReportsUsageError()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetQualityLevel", output);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(output.Errors[0], Does.Contain("Usage"));
        }

        /// <summary>範囲外のインデックスを指定した場合、エラーを報告することを検証します.</summary>
        [Test]
        public void SetQualityLevel_WithOutOfRangeIndex_ReportsError()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetQualityLevel", output);

            handler.Proc(new CommandArgument[] { new("999999") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
        }
    }
}

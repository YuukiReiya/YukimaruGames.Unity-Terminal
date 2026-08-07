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
    /// <see cref="TerminalGeneralCommands"/> が提供する組み込みコマンドを検証するテストクラス.
    /// </summary>
    [TestFixture]
    public sealed class TerminalGeneralCommandsTests
    {
        /// <summary>
        /// 出力を記録するだけの<see cref="IModeOutput"/>テストダブル.
        /// </summary>
        private sealed class RecordingOutput : IModeOutput
        {
            public readonly List<string> Messages = new();
            public readonly List<string> Errors = new();

            public void Message(string message) => Messages.Add(message);
            public void Warning(string message) { }
            public void Error(string message) => Errors.Add(message);
        }

        /// <summary>
        /// 固定のハンドラー一覧を返す<see cref="ICommandRegistry"/>テストダブル.
        /// </summary>
        private sealed class FixedRegistry : ICommandRegistry
        {
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
            var method = typeof(TerminalGeneralCommands).GetMethod(
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

        [Test]
        public void Echo_WithArguments_JoinsArgumentsWithSpace()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("Echo", output);

            handler.Proc(new CommandArgument[] { new("hello"), new("world") }.AsMemory());

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("hello world"));
        }

        [Test]
        public void Echo_WithoutArguments_PrintsEmptyMessage()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("Echo", output);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo(string.Empty));
        }

        [Test]
        public void ListCommands_WithRegisteredHandlers_PrintsNameAndHelpSortedOrdinally()
        {
            var output = new RecordingOutput();
            var registry = new FixedRegistry
            {
                All = new[]
                {
                    MakeStubHandler("echo", "Echoes text back."),
                    MakeStubHandler("commands", "Lists all registered commands."),
                    MakeStubHandler("fps", string.Empty),
                },
            };
            var handler = CreateHandler("ListCommands", output, registry);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(
                output.Messages[0],
                Is.EqualTo("commands - Lists all registered commands.\necho - Echoes text back.\nfps"));
        }

        [Test]
        public void ListCommands_WithNoRegisteredHandlers_PrintsPlaceholderMessage()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("ListCommands", output);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("No commands are registered."));
        }

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

        [Test]
        public void SetQualityLevel_WithoutArguments_ReportsUsageError()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetQualityLevel", output);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(output.Errors[0], Does.Contain("Usage"));
        }

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

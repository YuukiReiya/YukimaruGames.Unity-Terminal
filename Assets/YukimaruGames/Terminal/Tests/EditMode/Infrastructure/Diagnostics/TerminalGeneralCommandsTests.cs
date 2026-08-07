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
        /// 固定の登録済みワード一覧を返す<see cref="ICommandAutocomplete"/>テストダブル.
        /// </summary>
        private sealed class FixedAutocomplete : ICommandAutocomplete
        {
            public IEnumerable<string> KnownWords { get; set; } = Array.Empty<string>();
            public bool Register(string word) => true;
            public bool Unregister(string word) => true;
            public string[] Complete(string text) => Array.Empty<string>();
        }

        private static CommandHandler CreateHandler(string methodName, RecordingOutput output, FixedAutocomplete autocomplete)
        {
            var method = typeof(TerminalGeneralCommands).GetMethod(
                methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found.");

            var services = new Dictionary<Type, object>
            {
                { typeof(IModeOutput), output },
                { typeof(ICommandAutocomplete), autocomplete },
            };

            return CommandFactory.Create(method, new ModeServiceBundle(services));
        }

        [Test]
        public void Echo_WithArguments_JoinsArgumentsWithSpace()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("Echo", output, new FixedAutocomplete());

            handler.Proc(new CommandArgument[] { new("hello"), new("world") }.AsMemory());

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("hello world"));
        }

        [Test]
        public void Echo_WithoutArguments_PrintsEmptyMessage()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("Echo", output, new FixedAutocomplete());

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo(string.Empty));
        }

        [Test]
        public void ListCommands_WithRegisteredWords_PrintsThemSortedOrdinally()
        {
            var output = new RecordingOutput();
            var autocomplete = new FixedAutocomplete { KnownWords = new[] { "echo", "commands", "fps" } };
            var handler = CreateHandler("ListCommands", output, autocomplete);

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("commands\necho\nfps"));
        }

        [Test]
        public void ListCommands_WithNoRegisteredWords_PrintsPlaceholderMessage()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("ListCommands", output, new FixedAutocomplete());

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Messages, Has.Count.EqualTo(1));
            Assert.That(output.Messages[0], Is.EqualTo("No commands are registered."));
        }

        [Test]
        public void TimeScale_WithNegativeValue_ReportsErrorWithoutApplying()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("TimeScale", output, new FixedAutocomplete());
            var before = UnityEngine.Time.timeScale;

            handler.Proc(new CommandArgument[] { new("-1") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(UnityEngine.Time.timeScale, Is.EqualTo(before));
        }

        [Test]
        public void SetTargetFrameRate_WithPositiveValue_AppliesValue()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetTargetFrameRate", output, new FixedAutocomplete());
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
            var handler = CreateHandler("SetTargetFrameRate", output, new FixedAutocomplete());
            var before = UnityEngine.Application.targetFrameRate;

            handler.Proc(new CommandArgument[] { new("0") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(UnityEngine.Application.targetFrameRate, Is.EqualTo(before));
        }

        [Test]
        public void SetQualityLevel_WithoutArguments_ReportsUsageError()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetQualityLevel", output, new FixedAutocomplete());

            handler.Proc(ReadOnlyMemory<CommandArgument>.Empty);

            Assert.That(output.Errors, Has.Count.EqualTo(1));
            Assert.That(output.Errors[0], Does.Contain("Usage"));
        }

        [Test]
        public void SetQualityLevel_WithOutOfRangeIndex_ReportsError()
        {
            var output = new RecordingOutput();
            var handler = CreateHandler("SetQualityLevel", output, new FixedAutocomplete());

            handler.Proc(new CommandArgument[] { new("999999") }.AsMemory());

            Assert.That(output.Errors, Has.Count.EqualTo(1));
        }
    }
}

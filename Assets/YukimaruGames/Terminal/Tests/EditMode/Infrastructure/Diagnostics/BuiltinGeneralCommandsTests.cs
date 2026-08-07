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

        /// <summary>登録済みハンドラーがある場合、コマンド名の辞書順で「名前 - ヘルプ」形式に整形されることを検証します.</summary>
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

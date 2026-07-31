using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Abstractions.Attributes;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Abstractions.Models.Entities;
using YukimaruGames.Terminal.Infrastructure.Discoverer;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Infrastructure.Discovery
{
    [TestFixture]
    public sealed class CommandDiscovererTests
    {
        private const string TestAssemblyName = "YukimaruGames.Terminal.Tests.EditMode";

        private sealed class MockCommandLogger : ICommandLogger
        {
            public int MaxLogs => 100;
            public IReadOnlyCollection<CommandLog> Logs => Array.Empty<CommandLog>();
            public List<(MessageType type, string message)> Sent { get; } = new();

            public event Action OnItemUpdated;
            public event Action<CommandLog[]> OnItemAdded;
            public event Action<CommandLog[]> OnItemRemoved;

            public void Clear()
            {
            }

            public void Send(MessageType msgType, string message) => Sent.Add((msgType, message));
        }

        [TerminalCommand("discoverertest.sample", maxArgCount: 1, minArgCount: 0, help: "sample")]
        private static void SampleCommand(string arg)
        {
        }

        // ReSharper disable once UnusedMember.Local
        [TerminalCommand("")]
        private static void EmptyCommandName()
        {
        }

        // ReSharper disable once UnusedMember.Local
        [TerminalCommand("discoverertest.instance")]
        private void InstanceCommand()
        {
        }

        [Test]
        public void Discover_FindsStaticMethodWithAttribute()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger, new[] { TestAssemblyName });

            var specs = discoverer.Discover().ToArray();

            var found = specs.FirstOrDefault(s => s.Meta.Command == "discoverertest.sample");
            Assert.That(found.Meta.Command, Is.EqualTo("discoverertest.sample"));
            Assert.That(found.Method.Name, Is.EqualTo(nameof(SampleCommand)));
        }

        [Test]
        public void Discover_SkipsMethodWithEmptyCommandName()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger, new[] { TestAssemblyName });

            var specs = discoverer.Discover().ToArray();

            Assert.That(specs.Any(s => s.Method.Name == nameof(EmptyCommandName)), Is.False);
        }

        [Test]
        public void Discover_SkipsInstanceMethod()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger, new[] { TestAssemblyName });

            var specs = discoverer.Discover().ToArray();

            Assert.That(specs.Any(s => s.Method.Name == nameof(InstanceCommand)), Is.False);
        }

        [Test]
        public void Discover_UnknownAssembly_LogsExceptionAndRethrows()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger, new[] { "NonExistentAssembly.DoesNotExist" });

            Assert.Throws<System.IO.FileNotFoundException>(() => discoverer.Discover().ToArray());
            Assert.That(logger.Sent.Any(s => s.type == MessageType.Exception), Is.True);
        }

        [Test]
        public void DefaultConstructor_UsesAssemblyCSharp()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            Assert.DoesNotThrow(() => discoverer.Discover().ToArray());
        }
    }
}

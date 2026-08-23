using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.Entities;
using YukimaruGames.Terminal.Infrastructure.Discoverer;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Infrastructure.Discovery
{
    /// <summary>
    /// <see cref="CommandDiscoverer"/> のコマンド検出動作を検証するテストクラス。
    /// </summary>
    [TestFixture]
    public sealed class CommandDiscovererTests
    {
        private sealed class MockCommandLogger : ICommandLogger
        {
            public int MaxLogs => 100;
            public IReadOnlyCollection<CommandLog> Logs => Array.Empty<CommandLog>();
            public List<(MessageType type, string message)> Sent { get; } = new();

            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action OnItemUpdated { add { } remove { } }
            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<CommandLog[]> OnItemAdded { add { } remove { } }
            // ReSharper disable once EventNeverSubscribedTo.Local
            public event Action<CommandLog[]> OnItemRemoved { add { } remove { } }

            /// <summary>
            /// このモックではログを保持しないため何も行いません。
            /// </summary>
            public void Clear()
            {
            }

            /// <summary>
            /// 送信されたメッセージを <see cref="Sent"/> に記録します。
            /// </summary>
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

        /// <summary>
        /// 静的メソッドに付与された <see cref="TerminalCommandAttribute"/> が検出され、
        /// メタ情報とメソッド情報が正しく取得できることを検証します。
        /// </summary>
        /// <remarks>
        /// このテストクラス自体が(Assembly-CSharpではなく)独自asmdef
        /// <c>YukimaruGames.Terminal.Tests.EditMode</c> に属しているため、手動でのアセンブリ指定
        /// 無しに独自asmdef配下のコマンドが発見できることも同時に検証している(#176).
        /// </remarks>
        [Test]
        public void Discover_FindsStaticMethodWithAttribute()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.Discover().ToArray();

            Assert.That(specs.Any(s => s.Meta.Command == "discoverertest.sample"), Is.True);

            var found = specs.First(s => s.Meta.Command == "discoverertest.sample");
            Assert.That(found.Meta.Command, Is.EqualTo("discoverertest.sample"));
            Assert.That(found.Method.Name, Is.EqualTo(nameof(SampleCommand)));
        }

        /// <summary>
        /// コマンド名が空のメソッドは検出結果から除外されることを検証します。
        /// </summary>
        [Test]
        public void Discover_SkipsMethodWithEmptyCommandName()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.Discover().ToArray();

            Assert.That(specs.Any(s => s.Method.Name == nameof(EmptyCommandName)), Is.False);
        }

        /// <summary>
        /// インスタンスメソッドは検出結果から除外されることを検証します。
        /// </summary>
        [Test]
        public void Discover_SkipsInstanceMethod()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.Discover().ToArray();

            Assert.That(specs.Any(s => s.Method.Name == nameof(InstanceCommand)), Is.False);
        }

        /// <summary>
        /// <see cref="TerminalCommandAttribute"/>の定義アセンブリを参照していないアセンブリ
        /// (例: mscorlib/System等)が混ざっていても、例外を送出せず動作することを検証します。
        /// </summary>
        [Test]
        public void Discover_IgnoresAssembliesNotReferencingAttributeAssembly()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            Assert.DoesNotThrow(() => discoverer.Discover().ToArray());
            Assert.That(logger.Sent.Any(s => s.type == MessageType.Exception), Is.False);
        }

        // ─── DiscoverModeCommands ────────────────────────────────────────────

        private class BaseMode
        {
            [TerminalModeCommand(typeof(BaseMode), "shared.help")]
            public void Help()
            {
            }

            [TerminalModeCommand(typeof(BaseMode), "shared.overridden")]
            public virtual void Overridden()
            {
            }
        }

        private sealed class DerivedMode : BaseMode
        {
            [TerminalModeCommand(typeof(DerivedMode), "derived.only")]
            public void DerivedOnly()
            {
            }

            [TerminalModeCommand(typeof(DerivedMode), "shared.overridden")]
            public override void Overridden()
            {
            }
        }

        private sealed class UnrelatedMode
        {
            [TerminalModeCommand("string-id-mode", "byid.command")]
            public void ById()
            {
            }
        }

        /// <summary>
        /// 基底クラスに宣言されたコマンドが、派生モードでも発見されることを検証します。
        /// </summary>
        [Test]
        public void DiscoverModeCommands_FindsCommandsDeclaredOnBaseClass()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.DiscoverModeCommands(typeof(DerivedMode), "derived");

            Assert.That(specs.Any(s => s.Meta.Command == "shared.help"), Is.True);
        }

        /// <summary>
        /// 派生クラス自身のコマンドも発見されることを検証します。
        /// </summary>
        [Test]
        public void DiscoverModeCommands_FindsCommandsDeclaredOnDerivedClass()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.DiscoverModeCommands(typeof(DerivedMode), "derived");

            Assert.That(specs.Any(s => s.Meta.Command == "derived.only"), Is.True);
        }

        /// <summary>
        /// override されたメソッドは、派生側の属性・宣言だけが採用され重複しないことを検証します。
        /// </summary>
        [Test]
        public void DiscoverModeCommands_OverriddenMethod_PrefersDerivedDeclaration()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.DiscoverModeCommands(typeof(DerivedMode), "derived");

            var matches = specs.Where(s => s.Meta.Command == "shared.overridden").ToArray();
            Assert.That(matches.Length, Is.EqualTo(1));
            Assert.That(matches[0].Method.DeclaringType, Is.EqualTo(typeof(DerivedMode)));
        }

        /// <summary>
        /// 基底クラス限定(typeof(BaseMode))で探索した場合、派生専用コマンドは含まれないことを検証します。
        /// </summary>
        [Test]
        public void DiscoverModeCommands_BaseTypeOnly_DoesNotIncludeDerivedOnlyCommand()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.DiscoverModeCommands(typeof(BaseMode), "base");

            Assert.That(specs.Any(s => s.Meta.Command == "derived.only"), Is.False);
        }

        /// <summary>
        /// 文字列ID指定の属性は、modeIdが一致した場合にのみ発見されることを検証します。
        /// </summary>
        [Test]
        public void DiscoverModeCommands_StringId_MatchesOnlyWhenIdEqual()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var matched = discoverer.DiscoverModeCommands(typeof(UnrelatedMode), "string-id-mode");
            var unmatched = discoverer.DiscoverModeCommands(typeof(UnrelatedMode), "different-id");

            Assert.That(matched.Any(s => s.Meta.Command == "byid.command"), Is.True);
            Assert.That(unmatched.Any(s => s.Meta.Command == "byid.command"), Is.False);
        }

        /// <summary>
        /// 無関係なモード型では、[TerminalModeCommand(typeof(BaseMode))]なコマンドは発見されないことを検証します。
        /// </summary>
        [Test]
        public void DiscoverModeCommands_UnrelatedType_DoesNotMatch()
        {
            var logger = new MockCommandLogger();
            var discoverer = new CommandDiscoverer(logger);

            var specs = discoverer.DiscoverModeCommands(typeof(UnrelatedMode), "unrelated");

            Assert.That(specs.Any(s => s.Meta.Command == "shared.help"), Is.False);
        }
    }
}

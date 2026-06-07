using System.Threading.Tasks;
using NUnit.Framework;
using YukimaruGames.Terminal.Domain.Abstractions.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Services;

namespace YukimaruGames.Terminal.Tests.EditMode.Domain.Services
{
    [TestFixture]
    public sealed class CommandParserTests
    {
        private CommandParser _parser;

        [SetUp]
        public void SetUp()
        {
            _parser = new CommandParser();
        }

        [Test]
        public void Parse_EmptyInput_ReturnsMalformedInput()
        {
            var result = _parser.Parse(string.Empty, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.MalformedInput, result);
            Assert.IsNull(tuple.Command);
            Assert.IsNull(tuple.Arguments);
        }

        [Test]
        public void Parse_CommandOnly_ReturnsCommandWithoutArguments()
        {
            var result = _parser.Parse("help", out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual("help", tuple.Command);
            Assert.IsNotNull(tuple.Arguments);
            Assert.AreEqual(0, tuple.Arguments.Length);
        }

        [Test]
        public void Parse_SplitsArguments_WithoutAllocatingIntermediateStrings()
        {
            var result = _parser.Parse("echo alpha beta", out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual("echo", tuple.Command);
            Assert.AreEqual(2, tuple.Arguments.Length);
            Assert.AreEqual("alpha", tuple.Arguments[0].String);
            Assert.AreEqual("beta", tuple.Arguments[1].String);
        }

        [Test]
        public void Parse_QuotedArgument_AllowsWhitespaceInside()
        {
            var result = _parser.Parse("say \"hello world\" tail", out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual("say", tuple.Command);
            Assert.AreEqual(2, tuple.Arguments.Length);
            Assert.AreEqual("hello world", tuple.Arguments[0].String);
            Assert.AreEqual("tail", tuple.Arguments[1].String);
        }

        [Test]
        public void Parse_UnclosedQuote_ReturnsSyntaxError()
        {
            var result = _parser.Parse("say \"hello world", out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.SyntaxError, result);
            Assert.AreEqual("say", tuple.Command);
            Assert.IsNull(tuple.Arguments);
        }

        [Test]
        public async Task ParseAsync_ReadOnlyMemory_Works()
        {
            var result = await _parser.ParseAsync("move 1 2".AsMemory());

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result.Status);
            Assert.AreEqual("move", result.Command);
            Assert.AreEqual(2, result.Arguments.Length);
            Assert.AreEqual("1", result.Arguments[0].String);
            Assert.AreEqual("2", result.Arguments[1].String);
        }
    }
}

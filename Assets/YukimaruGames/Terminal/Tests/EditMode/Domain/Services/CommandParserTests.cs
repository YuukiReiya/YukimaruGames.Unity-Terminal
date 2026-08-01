using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using YukimaruGames.Terminal.Domain.Contracts.Interfaces.Services;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Services;
using CommandHandler = YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects.CommandHandler;

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

        [TestCaseSource(nameof(NoContentStringsTestCase))]
        public void Parse_NoContent_ReturnsMalformedInput(string input)
        {
            var result = _parser.Parse(input, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.MalformedInput, result);
            Assert.IsNull(tuple.Command);
            Assert.IsNull(tuple.Arguments);
        }

        [TestCaseSource(nameof(NoArgsStringsTestCase))]
        public void Parse_NoArgs_ReturnsCommandWithoutArguments(string input, string expectedCommand)
        {
            var result = _parser.Parse(input, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual(expectedCommand, tuple.Command);
            Assert.IsNotNull(tuple.Arguments);
            Assert.AreEqual(0, tuple.Arguments.Length);
        }

        [TestCaseSource(nameof(Args1StringsTestCase))]
        public void Parse_OneArg_ReturnsExpectedArgument(string input, string expectedCommand, string expectedArg1)
        {
            var result = _parser.Parse(input, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual(expectedCommand, tuple.Command);
            Assert.AreEqual(1, tuple.Arguments.Length);
            Assert.AreEqual(expectedArg1, tuple.Arguments[0].String);
        }

        [TestCaseSource(nameof(Args2StringsTestCase))]
        public void Parse_TwoArgs_ReturnsExpectedArguments(string input, string expectedCommand, string expectedArg1, string expectedArg2)
        {
            var result = _parser.Parse(input, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual(expectedCommand, tuple.Command);
            Assert.AreEqual(2, tuple.Arguments.Length);
            Assert.AreEqual(expectedArg1, tuple.Arguments[0].String);
            Assert.AreEqual(expectedArg2, tuple.Arguments[1].String);
        }

        [TestCaseSource(nameof(QuotedArgsTestCase))]
        public void Parse_QuotedArguments_ReturnsExpectedArguments(string input, string expectedCommand, string[] expectedArgs)
        {
            var result = _parser.Parse(input, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result);
            Assert.AreEqual(expectedCommand, tuple.Command);
            Assert.AreEqual(expectedArgs.Length, tuple.Arguments.Length);

            for (var i = 0; i < expectedArgs.Length; i++)
            {
                Assert.AreEqual(expectedArgs[i], tuple.Arguments[i].String);
            }
        }

        [TestCaseSource(nameof(InvalidQuotedArgsTestCase))]
        public void Parse_InvalidQuotedArguments_ReturnsSyntaxError(string input, string expectedCommand)
        {
            var result = _parser.Parse(input, out var tuple);

            Assert.AreEqual(ICommandParser.ParseStatusCode.SyntaxError, result);
            Assert.AreEqual(expectedCommand, tuple.Command);
            Assert.IsEmpty(tuple.Arguments);
        }

        [Test]
        public async Task ParseAsync_ReadOnlyMemory_Works()
        {
            using var cts = new CancellationTokenSource();
            var result = await _parser.ParseAsync("move 1 2".AsMemory(), cts.Token);

            Assert.AreEqual(ICommandParser.ParseStatusCode.Ok, result.Status);
            Assert.AreEqual("move", result.Command);
            Assert.AreEqual(2, result.Arguments.Length);
            Assert.AreEqual("1", result.Arguments[0].String);
            Assert.AreEqual("2", result.Arguments[1].String);
        }

        private static TestCaseData[] NoContentStringsTestCase() =>
            new[]
            {
                new TestCaseData(null).SetName("Parse_NoContent_Null_ReturnsMalformedInput"),
                new TestCaseData(string.Empty).SetName("Parse_NoContent_Empty_ReturnsMalformedInput"),
                new TestCaseData(" ").SetName("Parse_NoContent_Space_ReturnsMalformedInput"),
                new TestCaseData("   ").SetName("Parse_NoContent_MultipleSpaces_ReturnsMalformedInput"),
                new TestCaseData("　").SetName("Parse_NoContent_WidthSpace_ReturnsMalformedInput"),
                new TestCaseData("\t").SetName("Parse_NoContent_Tab_ReturnsMalformedInput"),
            };

        private static TestCaseData[] NoArgsStringsTestCase() =>
            new[]
            {
                new TestCaseData("str", "str").SetName("Parse_NoArgs_CommandOnly_ReturnsCommand"),
                new TestCaseData("str ", "str").SetName("Parse_NoArgs_TrailingSpace_ReturnsCommand"),
                new TestCaseData("str　", "str").SetName("Parse_NoArgs_TrailingWideSpace_ReturnsCommand"),
                new TestCaseData("str\t", "str").SetName("Parse_NoArgs_TrailingTab_ReturnsCommand"),
                new TestCaseData(" str", "str").SetName("Parse_NoArgs_LeadingSpace_ReturnsCommand"),
                new TestCaseData("　str", "str").SetName("Parse_NoArgs_LeadingWideSpace_ReturnsCommand"),
                new TestCaseData("\tstr", "str").SetName("Parse_NoArgs_LeadingTab_ReturnsCommand"),
                new TestCaseData(" str ", "str").SetName("Parse_NoArgs_BothSidesSpace_ReturnsCommand"),
                new TestCaseData("　str　", "str").SetName("Parse_NoArgs_BothSidesWideSpace_ReturnsCommand"),
                new TestCaseData("\tstr\t", "str").SetName("Parse_NoArgs_BothSidesTab_ReturnsCommand"),
            };

        private static TestCaseData[] Args1StringsTestCase() =>
            new[]
            {
                new TestCaseData("str arg1", "str", "arg1").SetName("Parse_OneArg_SingleSpace"),
                new TestCaseData("str  arg1", "str", "arg1").SetName("Parse_OneArg_DoubleSpace"),
                new TestCaseData("str　arg1", "str", "arg1").SetName("Parse_OneArg_WideSpace"),
                new TestCaseData("str\targ1", "str", "arg1").SetName("Parse_OneArg_Tab"),
                new TestCaseData(" str arg1", "str", "arg1").SetName("Parse_OneArg_LeadingSpace"),
                new TestCaseData("\tstr\targ1\t", "str", "arg1").SetName("Parse_OneArg_LeadingAndTrailingTab"),
                new TestCaseData("str arg1 ", "str", "arg1").SetName("Parse_OneArg_TrailingSpace"),
                new TestCaseData("str　arg1　", "str", "arg1").SetName("Parse_OneArg_TrailingWideSpace"),
            };

        private static TestCaseData[] Args2StringsTestCase() =>
            new[]
            {
                new TestCaseData("str arg1 arg2", "str", "arg1", "arg2").SetName("Parse_TwoArgs_SingleSpace"),
                new TestCaseData(" str arg1 arg2 ", "str", "arg1", "arg2").SetName("Parse_TwoArgs_LeadingAndTrailingSpace"),
                new TestCaseData("str  arg1  arg2", "str", "arg1", "arg2").SetName("Parse_TwoArgs_DoubleSpace"),
                new TestCaseData("str　arg1　arg2", "str", "arg1", "arg2").SetName("Parse_TwoArgs_WideSpace"),
                new TestCaseData("str\targ1\targ2\t", "str", "arg1", "arg2").SetName("Parse_TwoArgs_Tab"),
            };

        private static TestCaseData[] QuotedArgsTestCase() =>
            new[]
            {
                new TestCaseData("str 'arg1' 'arg2'", "str", new[] { "arg1", "arg2" }).SetName("Parse_QuotedArgs_SingleQuotePair"),
                new TestCaseData("str \"arg 1\" \"arg 2\"", "str", new[] { "arg 1", "arg 2" }).SetName("Parse_QuotedArgs_DoubleQuotePair"),
                new TestCaseData("str \"arg 1\" 'arg 2'", "str", new[] { "arg 1", "arg 2" }).SetName("Parse_QuotedArgs_MixedQuotes"),
                new TestCaseData("str 'arg 1' \"arg 2\"", "str", new[] { "arg 1", "arg 2" }).SetName("Parse_QuotedArgs_MixedQuotesReverse"),
                new TestCaseData("str \"printf('arg1')\"", "str", new[] { "printf('arg1')" }).SetName("Parse_QuotedArgs_NestedSingleInsideDouble"),
                new TestCaseData("str 'printf(\"arg1\")'", "str", new[] { "printf(\"arg1\")" }).SetName("Parse_QuotedArgs_NestedDoubleInsideSingle"),
                new TestCaseData("  str  \"hello world\"  tail  ", "str", new[] { "hello world", "tail" }).SetName("Parse_QuotedArgs_LeadingTrailingSpaces"),
            };

        private static TestCaseData[] InvalidQuotedArgsTestCase() =>
            new[]
            {
                new TestCaseData("str '", "str").SetName("Parse_InvalidQuotedArgs_SingleQuoteOpen"),
                new TestCaseData("str \"", "str").SetName("Parse_InvalidQuotedArgs_DoubleQuoteOpen"),
                new TestCaseData("str 'arg", "str").SetName("Parse_InvalidQuotedArgs_SingleQuoteUnclosed"),
                new TestCaseData("str \"arg", "str").SetName("Parse_InvalidQuotedArgs_DoubleQuoteUnclosed"),
                new TestCaseData(" str 'method \"arg", "str").SetName("Parse_InvalidQuotedArgs_MixedUnclosed"),
            };
    }
}

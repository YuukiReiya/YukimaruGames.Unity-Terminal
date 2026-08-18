using System;
using NUnit.Framework;
using YukimaruGames.Terminal.Adapters.CommandLine;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.EditMode.Adapters.CommandLine
{
    /// <summary>
    /// 送信元のターミナルへ入力行のエコーを返さない判定を検証する.
    /// </summary>
    /// <remarks>
    /// コマンド実行時、Unity側は入力文字列を<see cref="MessageType.Entry"/>としてログへ積む。
    /// これを送信元にも配信すると、そのターミナルには自分が打った入力行が既に表示されているため
    /// 二重に見える(#165)。一方で実行結果や他のターミナルへの配信は止めてはならない.
    /// </remarks>
    [TestFixture]
    public sealed class CommandLineBridgeTests
    {
        private const string Command = "commands";

        private static LogEntry Entry(MessageType type, string message) =>
            new(0, type, DateTimeOffset.UnixEpoch, message);

        /// <summary>実行中のコマンドと同じ入力エコーは、送信元への配信対象から外れることを検証します.</summary>
        [Test]
        public void IsInputEcho_実行中のコマンドと同じEntryは真()
        {
            Assert.That(CommandLineBridge.IsInputEcho(Entry(MessageType.Entry, Command), Command), Is.True);
        }

        /// <summary>別のコマンドの入力エコーは配信されることを検証します(他のターミナルの打鍵).</summary>
        [Test]
        public void IsInputEcho_別のコマンドのEntryは偽()
        {
            Assert.That(CommandLineBridge.IsInputEcho(Entry(MessageType.Entry, "clear"), Command), Is.False);
        }

        /// <summary>
        /// 入力エコー以外のログは、文字列が一致していても配信されることを検証します.
        /// </summary>
        /// <remarks>
        /// 実行結果が偶然コマンド名と同じ文字列になることはありうる(<c>echo commands</c>等)。
        /// 種別を見ずに文字列だけで判定すると、その出力が送信元にだけ届かなくなる.
        /// </remarks>
        [Test]
        public void IsInputEcho_Entry以外は偽(
            [Values(MessageType.Message, MessageType.Error, MessageType.Warning, MessageType.Exception)]
            MessageType messageType)
        {
            Assert.That(CommandLineBridge.IsInputEcho(Entry(messageType, Command), Command), Is.False);
        }

        /// <summary>コマンドを実行していない間のログは、すべて配信されることを検証します.</summary>
        [Test]
        public void IsInputEcho_実行中でなければ偽()
        {
            Assert.That(CommandLineBridge.IsInputEcho(Entry(MessageType.Entry, Command), null), Is.False);
        }

        /// <summary>ログが無い場合に例外にならないことを検証します.</summary>
        [Test]
        public void IsInputEcho_ログがnullなら偽()
        {
            Assert.That(CommandLineBridge.IsInputEcho(null, Command), Is.False);
        }
    }
}

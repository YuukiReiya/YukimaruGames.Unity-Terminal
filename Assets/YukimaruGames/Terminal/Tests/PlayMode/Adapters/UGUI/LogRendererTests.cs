using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using YukimaruGames.Terminal.Adapters.UGUI.Renderers;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Log;
using YukimaruGames.Terminal.SharedKernel;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.UGUI
{
    /// <summary>
    /// <see cref="LogRenderer"/>の行の使い回しと表示内容の同期を検証する.
    /// </summary>
    /// <remarks>
    /// 行のGameObjectは<c>ObjectPool</c>で使い回すため、返却漏れ(消えるべき行が残る)や
    /// 状態のリセット漏れ(再利用時に前回の内容が残る)が表示の破綻として出る.
    /// </remarks>
    [TestFixture]
    public sealed class LogRendererTests
    {
        private const string MessageName = "log-line-message";
        private const string CopyButtonName = "log-line-copy-button";

        private GameObject _root;
        private RectTransform _content;
        private LogRenderer _logRenderer;
        private StubClipboardRenderer _clipboard;
        private StubColorPalette _palette;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Test Log Content", typeof(RectTransform));
            _content = (RectTransform)_root.transform;

            _clipboard = new StubClipboardRenderer();
            _palette = new StubColorPalette();

            _logRenderer = new LogRenderer(
                _content,
                _clipboard,
                _palette,
                new StubLauncherVisible(),
                Color.cyan)
            {
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),
                FontSize = 14,
            };
        }

        [TearDown]
        public void TearDown()
        {
            ((IDisposable)_logRenderer)?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        private void Render(params LogEntry[] entries) =>
            _logRenderer.Render(new LogRenderData(entries));

        private static LogEntry Entry(int id, string message, MessageType type = MessageType.Message) =>
            new(id, type, DateTimeOffset.UnixEpoch, message);

        /// <summary>表示中(アクティブ)のログ行だけを拾う.</summary>
        private IEnumerable<Text> ActiveMessages() =>
            _content.GetComponentsInChildren<Text>(true)
                .Where(t => t.gameObject.name == MessageName && t.transform.parent.gameObject.activeInHierarchy);

        [UnityTest]
        public IEnumerator Render_行数が減ると余剰行は表示されない()
        {
            Render(Entry(0, "one"), Entry(1, "two"), Entry(2, "three"));
            yield return null;
            Assert.That(ActiveMessages().Count(), Is.EqualTo(3), "前提: 3行表示されていること");

            Render(Entry(0, "one"));
            yield return null;

            Assert.That(ActiveMessages().Count(), Is.EqualTo(1), "余剰行がプールへ返却されていない");
        }

        [UnityTest]
        public IEnumerator Render_プールから再利用した行に前回の内容が残らない()
        {
            Render(Entry(0, "first"), Entry(1, "second"));
            yield return null;

            // 一度減らして返却させ、再度増やして同じ行を使い回させる.
            Render(Entry(0, "first"));
            yield return null;
            Render(Entry(0, "first"), Entry(1, "renewed"));
            yield return null;

            var messages = ActiveMessages().Select(t => t.text).ToArray();

            Assert.That(messages, Does.Contain("renewed"));
            Assert.That(messages, Does.Not.Contain("second"), "再利用した行に前回の内容が残っている");
        }

        [UnityTest]
        public IEnumerator Render_MessageTypeに応じた色がパレット経由で反映される(
            [Values(MessageType.Error, MessageType.Warning, MessageType.Message, MessageType.Exception)]
            MessageType messageType)
        {
            var expected = _palette[messageType.ToString()];

            Render(Entry(0, "colored", messageType));
            yield return null;

            Assert.That(ActiveMessages().Single().color, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator コピーボタンのクリックでクリップボードへ通知される()
        {
            const string message = "copy me";
            Render(Entry(0, message));
            yield return null;

            var button = _content.GetComponentsInChildren<Button>(true)
                .Single(b => b.gameObject.name == CopyButtonName);
            button.onClick.Invoke();
            yield return null;

            Assert.That(_clipboard.LastCopied, Is.EqualTo(message));
        }

        private sealed class StubClipboardRenderer : IClipboardRenderer
        {
            public string LastCopied { get; private set; }

            public event Action<string> OnClickButton;

            public void Render(string copyText)
            {
                LastCopied = copyText;
                OnClickButton?.Invoke(copyText);
            }
        }

        /// <summary>
        /// <see cref="MessageType"/>ごとに識別できる色を返すパレット.
        /// </summary>
        private sealed class StubColorPalette : IColorPaletteProvider
        {
            private readonly Dictionary<string, Color> _colors = new()
            {
                [Definitions.ThemeLabel.Error] = Color.red,
                [Definitions.ThemeLabel.Assert] = Color.magenta,
                [Definitions.ThemeLabel.Warning] = Color.yellow,
                [Definitions.ThemeLabel.Message] = Color.white,
                [Definitions.ThemeLabel.Exception] = Color.grey,
                [Definitions.ThemeLabel.Entry] = Color.green,
                [Definitions.ThemeLabel.System] = Color.blue,
            };

            public Color this[string key] => _colors.TryGetValue(key, out var color) ? color : Color.clear;
        }

        private sealed class StubLauncherVisible : ILauncherVisibleProvider
        {
            public bool IsVisible => true;
            public bool IsReverse => false;

#pragma warning disable CS0067
            public event Action<bool> OnVisibleChanged;
            public event Action<bool> OnReverseChanged;
#pragma warning restore CS0067
        }
    }
}

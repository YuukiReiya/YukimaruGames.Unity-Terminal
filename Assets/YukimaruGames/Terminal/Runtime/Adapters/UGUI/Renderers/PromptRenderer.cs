#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine;
using UnityEngine.UI;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Adapters.UGUI.Renderers
{
    /// <summary>
    /// uGUI(<see cref="Text"/>)によるプロンプト表示の描画を行う.
    /// </summary>
    public sealed class PromptRenderer : IPromptRenderer, IDisposable
    {
        private static readonly string[] DefaultLoadingIndicatorFrames = { "|", "/", "-", "\\" };
        private const float SpinnerFramesPerSecond = 8f;

        private readonly Text _label;
        private readonly ITerminalService _service;

        private string[] _loadingIndicatorFrames = DefaultLoadingIndicatorFrames;
        private string _cachedPrompt;

        /// <summary>コマンド実行中にローディングインジケーターを表示するか.</summary>
        public bool ShowLoadingIndicator { private get; set; } = true;

        /// <summary>ローディングインジケーターのアニメーションフレーム文字列.</summary>
        public string[] LoadingIndicatorFrames
        {
            set => _loadingIndicatorFrames = value is { Length: > 0 } ? value : DefaultLoadingIndicatorFrames;
        }

        public PromptRenderer(Text label, ITerminalService service)
        {
            _label = label;
            _service = service;
            _cachedPrompt = _service?.Prompt ?? string.Empty;
        }

        /// <summary>
        /// プロンプト表示(コマンド実行中はローディングインジケーター)を更新する.
        /// </summary>
        public void Render()
        {
            if (_label == null) return;

            if (IsLoading())
            {
                var frame = _loadingIndicatorFrames[(int)(Time.realtimeSinceStartup * SpinnerFramesPerSecond) % _loadingIndicatorFrames.Length];
                _label.text = frame;
                return;
            }

            var prompt = _service?.Prompt ?? string.Empty;
            if (!string.Equals(prompt, _cachedPrompt, StringComparison.Ordinal))
            {
                _cachedPrompt = prompt;
            }

            _label.text = _cachedPrompt;
        }

        private bool IsLoading() => ShowLoadingIndicator && _service is { IsExecuting: true };

        void IDisposable.Dispose()
        {
        }
    }
}
#endif

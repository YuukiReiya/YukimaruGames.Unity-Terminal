using System;
using UnityEngine;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Adapters.GUI.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Adapters.GUI.Renderers
{
    public sealed class PromptRenderer : IPromptRenderer, IDisposable
    {
        private static readonly string[] DefaultLoadingIndicatorFrames = { "|", "/", "-", "\\" };
        private const float SpinnerFramesPerSecond = 8f;

        private readonly IGUIStyleProvider _provider;
        private readonly ITerminalService _service;
        private Vector2 _promptSize;
        private Vector2 _spinnerMaxSize;

        private string _cachedPrompt;
        private string[] _loadingIndicatorFrames = DefaultLoadingIndicatorFrames;

        /// <inheritdoc/>
        public bool ShowLoadingIndicator { private get; set; } = true;

        /// <inheritdoc/>
        public string[] LoadingIndicatorFrames
        {
            set
            {
                // 呼び出し元(ImmediateModeOptions等)が同じ配列インスタンスの要素だけを
                // 書き換えて再設定するケースがあるため、参照の同一性では判定せず毎回再計算する.
                _loadingIndicatorFrames = value is { Length: > 0 } ? value : DefaultLoadingIndicatorFrames;
                _spinnerMaxSize = CalcMaxSpinnerSize(_provider);
            }
        }

        public PromptRenderer(IGUIStyleProvider provider, ITerminalService service)
        {
            _provider = provider;
            _service = service;
            _cachedPrompt = _service?.Prompt ?? string.Empty;
            _promptSize = CalcSize(_provider, _cachedPrompt);
            _spinnerMaxSize = CalcMaxSpinnerSize(_provider);
            _provider.OnStyleChanged += OnChangedStyle;
        }

        void IDisposable.Dispose()
        {
            if (_provider != null)
            {
                _provider.OnStyleChanged -= OnChangedStyle;
            }
        }

        public void Render()
        {
            // 実行中はローディング表現に差し替え、プロンプトとの連結でユーザー入力のように
            // 見えてしまうのを避けるため、プロンプトとローディング表現は排他的に描画する.
            if (IsLoading())
            {
                RenderLoadingIndicator();
                return;
            }

            var prompt = _service?.Prompt ?? string.Empty;
            if (!string.Equals(prompt, _cachedPrompt, StringComparison.Ordinal))
            {
                _cachedPrompt = prompt;
                _promptSize = CalcSize(_provider, prompt);
            }

            if (!string.IsNullOrWhiteSpace(_cachedPrompt))
            {
                GUILayout.Label(_cachedPrompt, _provider.GetStyle(), GUILayout.Width(_promptSize.x), GUILayout.Height(_promptSize.y));
            }
        }

        private bool IsLoading() => ShowLoadingIndicator && _service is { IsExecuting: true };

        private void RenderLoadingIndicator()
        {
            var frame = _loadingIndicatorFrames[(int)(Time.realtimeSinceStartup * SpinnerFramesPerSecond) % _loadingIndicatorFrames.Length];
            GUILayout.Label(frame, _provider.GetStyle(), GUILayout.Width(_spinnerMaxSize.x), GUILayout.Height(_spinnerMaxSize.y));
        }

        private Vector2 CalcSize(IGUIStyleProvider provider, string prompt) => provider?.GetStyle().CalcSize(new GUIContent(prompt)) ?? Vector2.zero;

        private Vector2 CalcMaxSpinnerSize(IGUIStyleProvider provider)
        {
            var max = Vector2.zero;
            foreach (var frame in _loadingIndicatorFrames)
            {
                var size = CalcSize(provider, frame);
                max = Vector2.Max(max, size);
            }

            return max;
        }

        private void OnChangedStyle()
        {
            _promptSize = CalcSize(_provider, _cachedPrompt);
            _spinnerMaxSize = CalcMaxSpinnerSize(_provider);
        }
    }
}

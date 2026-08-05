using System;
using UnityEngine;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;

namespace YukimaruGames.Terminal.Adapters.GUI.Renderers
{
    public sealed class PromptRenderer : IPromptRenderer, IDisposable
    {
        private static readonly string[] SpinnerFrames = { "|", "/", "-", "\\" };
        private const float SpinnerFramesPerSecond = 8f;

        private readonly IGUIStyleProvider _provider;
        private readonly ITerminalService _service;
        private Vector2 _promptSize;
        private Vector2 _spinnerMaxSize;

        private string _prompt = "$";

        public string Prompt
        {
            private get => _prompt;
            set
            {
                if (_prompt == value) return;
                _prompt = value;
                _promptSize = CalcSize(_provider, value);
            }
        }

        /// <inheritdoc/>
        public bool ShowLoadingIndicator { private get; set; } = true;

        public PromptRenderer(IGUIStyleProvider provider, ITerminalService service)
        {
            _provider = provider;
            _service = service;
            _promptSize = CalcSize(_provider, _prompt);
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
            if (!string.IsNullOrWhiteSpace(Prompt))
            {
                GUILayout.Label(Prompt, _provider.GetStyle(), GUILayout.Width(_promptSize.x), GUILayout.Height(_promptSize.y));
            }

            RenderLoadingIndicatorIfNeeded();
        }

        private void RenderLoadingIndicatorIfNeeded()
        {
            if (!ShowLoadingIndicator) return;
            if (_service is not { IsExecuting: true }) return;

            var frame = SpinnerFrames[(int)(Time.realtimeSinceStartup * SpinnerFramesPerSecond) % SpinnerFrames.Length];
            GUILayout.Label(frame, _provider.GetStyle(), GUILayout.Width(_spinnerMaxSize.x), GUILayout.Height(_spinnerMaxSize.y));
        }

        private Vector2 CalcSize(IGUIStyleProvider provider, string prompt) => provider?.GetStyle().CalcSize(new GUIContent(prompt)) ?? Vector2.zero;

        private Vector2 CalcMaxSpinnerSize(IGUIStyleProvider provider)
        {
            var max = Vector2.zero;
            foreach (var frame in SpinnerFrames)
            {
                var size = CalcSize(provider, frame);
                max = Vector2.Max(max, size);
            }

            return max;
        }

        private void OnChangedStyle()
        {
            _promptSize = CalcSize(_provider, Prompt);
            _spinnerMaxSize = CalcMaxSpinnerSize(_provider);
        }
    }
}
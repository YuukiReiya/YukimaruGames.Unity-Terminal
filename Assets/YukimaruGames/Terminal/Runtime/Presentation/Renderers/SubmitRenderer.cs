using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Submit;

namespace YukimaruGames.Terminal.Presentation.Renderers
{
    public sealed class SubmitRenderer : ISubmitRenderer, IDisposable
    {
        private readonly IGUIStyleProvider _provider;

        private float _textLayoutWidth;
        
        /// <summary>
        /// 再計算するか
        /// </summary>
        /// <remarks>
        /// <p>初期値を true に設定することで初回描画時に遅延実行を促し適切なサイズを計算させる</p>
        /// <p>※コンストラクタで初期化すると正しいScreen.Sizeが入っておらず意図したサイズに計算されないため</p>
        /// </remarks>
        private bool _shouldRecalculation = true;

        private float TextLayoutWidth
        {
            get
            {
                if (_shouldRecalculation)
                {
                    _textLayoutWidth = CalcLayoutWidth();
                    _shouldRecalculation = false;
                }
                
                return _textLayoutWidth;
            }
        }

        /// <inheritdoc/>
        public string DisplayText => "| exec";

        /// <inheritdoc/>
        public event Action OnClickButton;

        public SubmitRenderer(IGUIStyleProvider provider)
        {
            _provider = provider;
            _provider.OnStyleChanged += OnStyleChanged;
        }

        /// <inheritdoc/>
        public void Render(SubmitRenderData renderData)
        {
            if (!renderData.IsVisible) return;

            if (GUILayout.Button(DisplayText, _provider.GetStyle(), GUILayout.Width(TextLayoutWidth), GUILayout.ExpandWidth(false)))
            {
                OnClickButton?.Invoke();
            }
        }

        /// <remarks>
        /// フォントサイズの動的な変更を考慮しスタイルが更新されたら再計算.
        /// </remarks>
        private void OnStyleChanged()
        {
            _shouldRecalculation = true;
        }

        private float CalcLayoutWidth() =>
            Mathf.Ceil(CalcTextSize().x);
        
        private Vector2 CalcTextSize() =>
            _provider.GetStyle().CalcSize(new GUIContent(DisplayText));

        void IDisposable.Dispose()
        {
            OnClickButton = null;
            _provider.OnStyleChanged -= OnStyleChanged;
        }
    }
}
#if TERMINAL_UGUI_AVAILABLE
using System;
using UnityEngine.UI;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Submit;

namespace YukimaruGames.Terminal.Adapters.UGUI.Renderers
{
    /// <summary>
    /// uGUI(<see cref="Button"/>)による実行ボタンの描画を行う.
    /// </summary>
    public sealed class SubmitRenderer : ISubmitRenderer, IDisposable
    {
        private readonly Button _button;
        private readonly Text _label;

        /// <inheritdoc/>
        public string DisplayText => "| exec";

        /// <inheritdoc/>
        public event Action OnClickButton;

        public SubmitRenderer(Button button)
        {
            _button = button;

            if (_button == null) return;

            // ボタン配下のTextは、Prefab側でもコード生成側でも子として作られる.
            _label = _button.GetComponentInChildren<Text>(true);
            if (_label != null) _label.text = DisplayText;

            _button.onClick.AddListener(HandleClicked);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// 非表示時はGameObjectごと無効化し、入力行のレイアウトから外す
        /// (UIToolkit版の<c>display: None</c>と同じ扱い)。
        /// ログ行のコピーボタンは同じ行のテキスト折り返し幅が変わって高さ計算が振動するため
        /// 別の方法で隠しているが、実行ボタンは行内で独立した要素のためその問題は起きない.
        /// </remarks>
        public void Render(SubmitRenderData renderData)
        {
            if (_button == null) return;

            if (_button.gameObject.activeSelf != renderData.IsVisible)
            {
                _button.gameObject.SetActive(renderData.IsVisible);
            }
        }

        private void HandleClicked() => OnClickButton?.Invoke();

        void IDisposable.Dispose()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }

            OnClickButton = null;
        }
    }
}
#endif

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace YukimaruGames.Terminal.Tests.PlayMode.Adapters.UIToolkit
{
    /// <summary>
    /// テストからUIToolkitの要素を操作するための、使い捨てのパネル.
    /// </summary>
    /// <remarks>
    /// <see cref="VisualElement"/>への疑似入力は<b>パネルへ接続済みの要素にしか届かない</b>
    /// (イベントの送出はpanel経由で行われるため)。実行時に<see cref="PanelSettings"/>と
    /// <see cref="UIDocument"/>を組み立て、テストが要素を差し込めるようにする(#127)。
    /// <para>
    /// スケールモードはピクセル等倍にする。既定の<see cref="PanelScaleMode.ConstantPhysicalSize"/>は
    /// 実行環境のDPIで座標が変わるため、座標を指定する疑似入力の結果が環境依存になる.
    /// </para>
    /// </remarks>
    internal sealed class UIToolkitTestPanel : IDisposable
    {
        private readonly GameObject _gameObject;
        private readonly PanelSettings _panelSettings;

        internal UIToolkitTestPanel(string name = "UIToolkit Test Panel")
        {
            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;

            _gameObject = new GameObject(name, typeof(UIDocument));
            var document = _gameObject.GetComponent<UIDocument>();
            document.panelSettings = _panelSettings;

            Root = document.rootVisualElement;
        }

        /// <summary>テストが要素を差し込む先.</summary>
        internal VisualElement Root { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_gameObject != null) UnityEngine.Object.DestroyImmediate(_gameObject);
            if (_panelSettings != null) UnityEngine.Object.DestroyImmediate(_panelSettings);
        }
    }
}

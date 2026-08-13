using System;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 実行時に生成した<see cref="UnityEngine.Object"/>を、
    /// <see cref="TerminalRuntimeScope"/>の破棄に合わせて解放するためのハンドル.
    /// </summary>
    /// <remarks>
    /// GameObjectにアタッチされないScriptableObject等は、GameObjectを破棄しても解放されず、
    /// 明示的にDestroyしない限りEditorでのPlay Mode反復のたびにリークする。
    /// <see cref="UnityEngine.Object"/>自体は<see cref="IDisposable"/>ではないため、
    /// Scopeへ他のコンポーネントと同じ経路で破棄させるにはこのハンドルを介する。
    /// InstallerがRenderingContextのComponentsへ載せることで、Scopeの破棄で解放される。
    ///
    /// 生成物の所有者はこのハンドルであり、生成した側(ファクトリ)ではない。
    /// 構築の途中で例外が発生しComponentsが未確定になる場合に備え、Installerが保険として
    /// 直接<see cref="Dispose"/>することも想定しているため、多重呼び出しに耐えること(冪等).
    /// </remarks>
    internal sealed class RuntimeGeneratedAsset : IDisposable
    {
        private UnityEngine.Object _asset;

        internal RuntimeGeneratedAsset(UnityEngine.Object asset) => _asset = asset;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_asset == null)
            {
                _asset = null;
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_asset);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_asset);
            }

            _asset = null;
        }
    }
}

#if TERMINAL_UGUI_AVAILABLE
using UnityEngine;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// uGUIバックエンドの<see cref="UnityEngine.UI.CanvasScaler"/>へ渡す設定.
    /// </summary>
    /// <remarks>
    /// <see cref="UGUIScaleMode.AutoFetch"/>では<see cref="ReferenceResolution"/>と
    /// <see cref="MatchWidthOrHeight"/>を、<see cref="UGUIScaleMode.Fixed"/>では
    /// <see cref="ScaleFactor"/>を使う。どちらを使うかで無視される項目があるため、
    /// 引数を並べるのではなく1つの設定としてまとめて渡す.
    /// </remarks>
    public readonly struct CanvasScaleSettings
    {
        public UGUIScaleMode Mode { get; }
        public Vector2 ReferenceResolution { get; }
        public float MatchWidthOrHeight { get; }
        public float ScaleFactor { get; }

        public CanvasScaleSettings(
            UGUIScaleMode mode,
            Vector2 referenceResolution,
            float matchWidthOrHeight,
            float scaleFactor)
        {
            Mode = mode;
            ReferenceResolution = referenceResolution;
            MatchWidthOrHeight = matchWidthOrHeight;
            ScaleFactor = scaleFactor;
        }
    }
}
#endif

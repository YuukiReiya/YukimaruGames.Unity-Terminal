#if TERMINAL_UGUI_AVAILABLE
namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// uGUIバックエンドのUI拡大率の決め方.
    /// </summary>
    /// <remarks>
    /// フォントサイズやボタン幅は<c>Canvas</c>単位の絶対値で組み立てるため、拡大率を固定にすると
    /// 基準にした解像度以外で相対的に大きすぎたり小さすぎたりする。実行時の解像度から
    /// 自動算出できるようにするための切り替え.
    /// </remarks>
    public enum UGUIScaleMode
    {
        /// <summary>
        /// 実行時の解像度と基準解像度の比から自動算出する.
        /// </summary>
        AutoFetch = 0,

        /// <summary>
        /// 指定した拡大率をそのまま使う(1で「1px = 1px」).
        /// </summary>
        Fixed = 1,
    }
}
#endif

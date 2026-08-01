namespace YukimaruGames.Terminal.Presentation.Contracts
{
    /// <summary>
    /// カーソル表示に関するView操作契約.
    /// <para>
    /// Phase 6（Adapters）でIMGUI等の具体的な描画実装から接続される想定の契約定義。
    /// </para>
    /// </summary>
    public interface ICursorView
    {
        /// <summary>カーソルの表示/非表示を設定する.</summary>
        void SetVisible(bool visible);
    }
}

namespace YukimaruGames.Terminal.Presentation.Contracts
{
    /// <summary>
    /// ターミナルウィンドウ全体のView操作契約.
    /// <para>
    /// Phase 6（Adapters）でIMGUI/InputSystem等の具体的な描画実装から接続される想定の契約定義。
    /// </para>
    /// </summary>
    public interface ITerminalView
    {
        /// <summary>ウィンドウ全体の表示/非表示を設定する.</summary>
        void SetVisible(bool visible);
    }
}

using System;

namespace YukimaruGames.Terminal.Presentation.Interfaces.Events
{
    /// <summary>
    /// ウィンドウが表示されている間、入力欄がフォーカスを持つことで生じうる
    /// キーボード入力系の副作用を回避するためのガード.
    /// </summary>
    public interface IWindowFocusInputGuard
    {
        /// <summary>
        /// ウィンドウの表示区間を開始する。戻り値の<see cref="IDisposable"/>を
        /// ウィンドウが閉じるタイミングで破棄することで、区間内で行った変更を元に戻す.
        /// </summary>
        IDisposable BeginScope();
    }
}

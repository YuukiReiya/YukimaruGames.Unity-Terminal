using System;

namespace YukimaruGames.Terminal.Infrastructure.Discoverer
{
    /// <summary>
    /// <see cref="CommandDiscoverer"/>がこのメソッドに対して発見不可の警告ログを出すのを抑制する.
    /// </summary>
    /// <remarks>
    /// 発見可否の判定(<c>IsDiscoverable</c>)そのものには一切影響しない。意図的に不正な形状で
    /// 用意されたテストフィクスチャ用のメソッド(空のコマンド名等)が、テストアセンブリの外
    /// (nunit参照を持たないため<see cref="CommandDiscoverer"/>の走査対象から除外されない場所)に
    /// 置かれる場合に、実際のEditor/Player実行時へ警告ログが漏れ出るのを防ぐためだけに使う.
    /// このパッケージ内部専用であり、公開APIではない(<c>internal</c>).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class SuppressCommandDiscoveryWarningAttribute : Attribute
    {
    }
}

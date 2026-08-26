using System;

namespace YukimaruGames.Terminal.Composition.Shared
{
    /// <summary>
    /// 宣言している型ごとに既定の実装型が異なる<c>SerializeReference</c>フィールドに付ける.
    /// </summary>
    /// <remarks>
    /// Inspectorで所有者(このフィールドを持つ側)の型を切り替えると、切り替え前の値が
    /// JSON経由で引き継がれる。これは共通フィールドの設定を維持するための意図的な挙動だが、
    /// 「所有者ごとに既定の実装型が異なるフィールド」では、型が食い違ったまま引き継がれて
    /// しまう(例: <c>CommandLineInstaller</c>から<c>ImmediateModeInstaller</c>へ切り替えても
    /// <c>_options</c>が<c>CommandLineOptions</c>のまま残る)。
    /// <para>
    /// この属性を付けたフィールドは、引き継がれた値の型が新しい所有者の既定型と食い違う場合に
    /// 限り、既定値へ差し替えられる。型が一致する場合は何もしない(値の引き継ぎを維持する)ため、
    /// 既定型が同じ所有者どうしの切り替え(例: IMGUI ⇔ UIToolkit ⇔ uGUI。いずれも
    /// <c>ImmediateModeOptions</c>が既定)では従来どおり設定が保たれる。
    /// </para>
    /// <para>
    /// 実際の差し替えはEditor側(型選択ドロップダウンの確定時)でのみ行われ、
    /// ランタイムの動作には影響しない.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ResetOnTypeMismatchAttribute : Attribute
    {
    }
}

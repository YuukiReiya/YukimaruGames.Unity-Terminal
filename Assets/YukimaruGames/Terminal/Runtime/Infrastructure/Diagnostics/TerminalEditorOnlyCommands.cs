#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using YukimaruGames.Terminal.Domain.Contracts.Attributes;
using YukimaruGames.Terminal.Domain.Contracts.Models.ValueObjects;
using YukimaruGames.Terminal.Domain.Contracts.Modes;

namespace YukimaruGames.Terminal.Infrastructure.Diagnostics
{
    /// <summary>
    /// Unity Editor上でのみ意味を持つ組み込みコマンド群.
    /// </summary>
    /// <remarks>
    /// クラス全体を<c>UNITY_EDITOR</c>シンボルで囲むことで、シンボルが定義されない実機ビルドでは
    /// 型ごとコンパイル対象から除外される。登録側(<c>TerminalStandardInstaller</c>)の呼び出し箇所も
    /// 同様に<c>#if UNITY_EDITOR</c>で囲い、実機ビルドにこの型への参照自体が残らないようにする.
    /// </remarks>
    public static class TerminalEditorOnlyCommands
    {
        [TerminalCommand("editor.pause", help: "Toggles EditorApplication.isPaused.")]
        private static void TogglePause(IModeOutput output)
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
            output.Message($"EditorApplication.isPaused = {EditorApplication.isPaused}");
        }

        [TerminalCommand("editor.ping", maxArgCount: 1, minArgCount: 1, help: "Pings (selects and highlights) an asset in the Project window. Usage: editor.ping <assetPath>")]
        private static void PingAsset(CommandArgument[] args, IModeOutput output)
        {
            if (args.Length < 1)
            {
                output.Error("Usage: editor.ping <assetPath>");
                return;
            }

            var path = args[0].String;
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                output.Error($"Asset not found at path: {path}");
                return;
            }

            EditorGUIUtility.PingObject(asset);
            output.Message($"Pinged: {path}");
        }

        /// <summary>
        /// このクラスが提供するコマンドメソッド一覧.
        /// </summary>
        public static MethodInfo[] Methods { get; } =
        {
            typeof(TerminalEditorOnlyCommands).GetMethod(nameof(TogglePause), BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(TerminalEditorOnlyCommands).GetMethod(nameof(PingAsset), BindingFlags.NonPublic | BindingFlags.Static)!,
        };
    }
}
#endif

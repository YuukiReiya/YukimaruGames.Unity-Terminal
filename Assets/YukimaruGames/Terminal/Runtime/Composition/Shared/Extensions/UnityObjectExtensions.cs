using UnityEngine;
using UnityObject=UnityEngine.Object;

namespace YukimaruGames.Terminal.Composition.Shared.Extensions
{
    public static class UnityObjectExtensions
    {
        public static void Destroy(this UnityObject unityObject)
        {
            if (UnityEngine.Application.isPlaying)
            {
                UnityObject.Destroy(unityObject);
            }
            else
            {
                UnityObject.DestroyImmediate(unityObject);
            }
        }

        /// <summary>
        /// Installerの任意上書きフィールドを解決する.
        /// </summary>
        /// <remarks>
        /// 明示的な上書き値が無い場合、<paramref name="resourcePath"/>を
        /// <see cref="Resources.Load{T}(string)"/>でロードしてフォールバックする
        /// （UIバックエンド用サブパッケージの同梱デフォルトアセット規約。
        /// GUID直接参照はしない: <c>.clinerules/04-project-structure.md</c>参照）.
        /// </remarks>
        public static T OrResource<T>(this T overrideValue, string resourcePath) where T : UnityObject
        {
            return overrideValue != null ? overrideValue : Resources.Load<T>(resourcePath);
        }
    }
}

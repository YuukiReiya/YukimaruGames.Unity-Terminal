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

    }
}

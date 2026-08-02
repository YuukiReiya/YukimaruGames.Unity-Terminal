using System;

namespace YukimaruGames.Terminal.Composition.Shared
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SerializeInterfaceAttribute : UnityEngine.PropertyAttribute
    {
        public bool UseToStringAsLabel { get; }

        public SerializeInterfaceAttribute(bool useToStringAsLabel = true)
        {
            UseToStringAsLabel = useToStringAsLabel;
        }
    }
}

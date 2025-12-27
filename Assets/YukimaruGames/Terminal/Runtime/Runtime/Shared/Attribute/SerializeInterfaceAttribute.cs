using System;

namespace YukimaruGames.Terminal.SharedKernel
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SerializeInterfaceAttribute : UnityEngine.PropertyAttribute
    {
        public bool UseToStringAsLabel { get; }

        /// <summary>
        /// Initializes a new instance of SerializeInterfaceAttribute and sets whether the field's label should use the target object's ToString() value.
        /// </summary>
        /// <param name="useToStringAsLabel">If true, the object's ToString() value will be used as the label shown in the Unity inspector; otherwise a default label is used.</param>
        public SerializeInterfaceAttribute(bool useToStringAsLabel = true)
        {
            UseToStringAsLabel = useToStringAsLabel;
        }
    }
}
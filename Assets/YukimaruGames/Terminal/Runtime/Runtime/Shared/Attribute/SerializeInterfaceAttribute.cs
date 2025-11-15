using System;

namespace YukimaruGames.Terminal.SharedKernel
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SerializeInterfaceAttribute : UnityEngine.PropertyAttribute
    {
        public bool IsIncludeMono { get; }

        public SerializeInterfaceAttribute(bool isIncludeMono = true)
        {
            IsIncludeMono = isIncludeMono;
        }
    }
}

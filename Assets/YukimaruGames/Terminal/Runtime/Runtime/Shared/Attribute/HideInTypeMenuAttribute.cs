using System;

namespace YukimaruGames.Terminal.Runtime.Shared
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class HideInTypeMenuAttribute : Attribute
    {
    }
}

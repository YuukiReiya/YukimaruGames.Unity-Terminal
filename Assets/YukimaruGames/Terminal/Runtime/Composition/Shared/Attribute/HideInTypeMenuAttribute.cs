using System;

namespace YukimaruGames.Terminal.Composition.Shared
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public sealed class HideInTypeMenuAttribute : Attribute
    {
    }
}

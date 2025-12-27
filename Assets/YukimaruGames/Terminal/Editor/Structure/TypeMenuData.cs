using System;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Editor
{
    internal readonly struct TypeMenuData
    {
        internal readonly Type Type;
        internal readonly string[] Segments;
        internal readonly AddTypeMenuAttribute Attribute;

        internal TypeMenuData(Type type, string[] segments, AddTypeMenuAttribute attribute)
        {
            Type = type;
            Segments = segments;
            Attribute = attribute;
        }
    }
}

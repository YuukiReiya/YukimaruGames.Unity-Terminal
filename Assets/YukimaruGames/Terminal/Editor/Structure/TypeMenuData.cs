using System;
using YukimaruGames.Terminal.Runtime.Shared;

namespace YukimaruGames.Terminal.Editor
{
    internal readonly struct TypeMenuData
    {
        internal readonly Type Type;
        internal readonly string[] Segments;
        internal readonly AddTypeMenuAttribute Attribute;

        /// <summary>
        /// Initializes a new TypeMenuData with the specified type, menu path segments, and attribute.
        /// </summary>
        /// <param name="type">The System.Type represented by this entry.</param>
        /// <param name="segments">An array of menu path segments used to build the type's menu location.</param>
        /// <param name="attribute">The AddTypeMenuAttribute instance associated with the type.</param>
        internal TypeMenuData(Type type, string[] segments, AddTypeMenuAttribute attribute)
        {
            Type = type;
            Segments = segments;
            Attribute = attribute;
        }
    }
}